namespace VolumeIntelligence.Core;

public sealed record ProfileSettings(decimal RowSize = 10m, decimal ValueAreaFraction = 0.70m,
    VolumeUnit VolumeUnit = VolumeUnit.QuoteNotional, int MaximumRows = 100_000);
public sealed record ProfileRow(decimal LowerPrice, decimal UpperPrice, decimal Total, decimal Buy, decimal Sell,
    decimal Unknown, long EventCount, long? UnderlyingTradeCount)
{
    public decimal Delta => Buy - Sell;
}
public sealed record ProfileSnapshot(SessionWindow Session, string CanonicalSymbol, string QuoteAsset, VolumeUnit VolumeUnit,
    decimal TotalVolume, decimal BuyVolume, decimal SellVolume, decimal UnknownVolume, decimal Delta,
    decimal? Poc, decimal? Vah, decimal? Val, decimal ValueAreaVolume,
    long EventCount, long? UnderlyingTradeCount, IReadOnlyList<ProfileRow> Rows, IReadOnlyDictionary<string, decimal> SourceContributions,
    DataQuality Quality);

public sealed class VolumeProfile
{
    private readonly ProfileSettings settings;
    private readonly SourceSelection selection;
    private readonly SessionWindow session;
    private readonly SortedDictionary<decimal, RowAccumulator> rows = [];
    private readonly Dictionary<string, decimal> contributions = new(StringComparer.Ordinal);
    private DataQuality quality;
    public VolumeProfile(ProfileSettings settings, SourceSelection selection, SessionWindow session)
    {
        if (settings.RowSize <= 0 || settings.ValueAreaFraction is <= 0 or > 1 || settings.MaximumRows <= 0 || !Enum.IsDefined(settings.VolumeUnit))
            throw new ArgumentException("Invalid profile settings.");
        this.settings = settings; this.selection = selection; this.session = session;
    }

    public void Add(NormalizedTrade trade, FxQuote? fx = null)
    {
        if (!selection.Accepts(trade.Raw)) throw new InvalidDataException("Instrument/source mismatch; implicit market or quote mixing is prohibited.");
        if (trade.Raw.ExchangeTimestampUtc < session.StartUtc || trade.Raw.ExchangeTimestampUtc >= session.EndUtc)
            throw new InvalidDataException("Trade is outside this session.");
        decimal volume = settings.VolumeUnit switch
        {
            VolumeUnit.Base => trade.BaseQuantity,
            VolumeUnit.QuoteNotional => trade.QuoteNotional,
            VolumeUnit.UsdNormalized => TradeNormalizer.UsdNotional(trade, fx, TimeSpan.FromMinutes(5)),
            _ => throw new ArgumentException("Invalid volume unit.")
        };
        decimal bin = decimal.Floor(trade.Raw.Price / settings.RowSize) * settings.RowSize;
        if (rows.Count > 0 && (Math.Max(rows.Keys.Last(), bin) - Math.Min(rows.Keys.First(), bin)) / settings.RowSize + 1 > settings.MaximumRows)
            throw new InvalidDataException("Price span exceeds the configured maximum rows; choose a larger row size.");
        if (!rows.TryGetValue(bin, out var row)) rows[bin] = row = new RowAccumulator();
        row.Total += volume; row.Events++;
        if (trade.Raw.UnderlyingTradeCount is { } count) row.UnderlyingCount += count;
        else row.UnknownCount = true;
        switch (trade.Raw.AggressorSide)
        {
            case AggressorSide.Buy: row.Buy += volume; break;
            case AggressorSide.Sell: row.Sell += volume; break;
            default: row.Unknown += volume; quality |= DataQuality.UnknownAggressor; break;
        }
        quality |= trade.Raw.Quality;
        if (trade.Raw.IsAggregatedTrade) quality |= DataQuality.AggregatedTrade;
        string source = trade.Raw.Source.Identity;
        contributions[source] = contributions.GetValueOrDefault(source) + volume;
    }

    public ProfileSnapshot Snapshot()
    {
        var resultRows = rows.Select(x => new ProfileRow(x.Key, x.Key + settings.RowSize, x.Value.Total, x.Value.Buy,
            x.Value.Sell, x.Value.Unknown, x.Value.Events, x.Value.UnknownCount ? null : x.Value.UnderlyingCount)).ToArray();
        decimal total = resultRows.Sum(x => x.Total), buy = resultRows.Sum(x => x.Buy), sell = resultRows.Sum(x => x.Sell);
        decimal? poc = null, val = null, vah = null; decimal vaVolume = 0;
        if (rows.Count > 0)
        {
            // Lowest-price POC wins ties. VA expands one adjacent row at a time; ties expand down.
            decimal p = resultRows.OrderByDescending(x => x.Total).ThenBy(x => x.LowerPrice).First().LowerPrice;
            decimal lower = p, upper = p; vaVolume = rows[p].Total;
            decimal first = rows.Keys.First(), last = rows.Keys.Last();
            while (vaVolume < total * settings.ValueAreaFraction)
            {
                bool canDown = lower > first, canUp = upper < last;
                decimal down = canDown ? rows.GetValueOrDefault(lower - settings.RowSize)?.Total ?? 0 : -1;
                decimal up = canUp ? rows.GetValueOrDefault(upper + settings.RowSize)?.Total ?? 0 : -1;
                if (!canDown && !canUp) break;
                if (canDown && (!canUp || down >= up)) { lower -= settings.RowSize; vaVolume += down; }
                else { upper += settings.RowSize; vaVolume += up; }
            }
            poc = p; val = lower; vah = upper + settings.RowSize;
        }
        return new(session, selection.CanonicalSymbol, selection.QuoteAsset, settings.VolumeUnit, total, buy, sell,
            total - buy - sell, buy - sell, poc, vah, val, vaVolume,
            resultRows.Sum(x => x.EventCount), resultRows.Any(x => x.UnderlyingTradeCount is null) ? null : resultRows.Sum(x => x.UnderlyingTradeCount ?? 0),
            resultRows, new SortedDictionary<string, decimal>(contributions, StringComparer.Ordinal), quality);
    }

    private sealed class RowAccumulator
    {
        public decimal Total, Buy, Sell, Unknown;
        public long Events, UnderlyingCount;
        public bool UnknownCount;
    }
}
