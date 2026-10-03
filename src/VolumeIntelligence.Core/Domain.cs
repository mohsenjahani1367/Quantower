using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VolumeIntelligence.Core;

public enum MarketType { Spot, LinearPerpetual, InversePerpetual }
public enum AggressorSide { Unknown, Buy, Sell }
public enum QuantityUnit { Base, Contracts }
public enum VolumeUnit { Base, QuoteNotional, UsdNormalized }
public enum SessionKind { Daily, Weekly, Monthly, CustomDaily }
[Flags]
public enum DataQuality { None = 0, UnknownAggressor = 1, AggregatedTrade = 2, NativeIdentityUnverified = 4, GapBefore = 8, NativeAggregationUnverified = 16 }

public sealed record SourceKey(string Venue, string ConnectionId, MarketType MarketType, string VenueSymbol)
{
    public string Identity => $"{Venue}|{ConnectionId}|{MarketType}|{VenueSymbol}";
}

public sealed record TradeEvent(
    int SchemaVersion, SourceKey Source, string CanonicalSymbol, string BaseAsset, string QuoteAsset,
    string TradeId, DateTimeOffset ExchangeTimestampUtc, DateTimeOffset ReceiveTimestampUtc,
    decimal Price, decimal Quantity, QuantityUnit QuantityUnit, decimal ContractMultiplier,
    AggressorSide AggressorSide, bool IsAggregatedTrade, long? UnderlyingTradeCount,
    long? SourceSequence, string? RawPayloadReference, DataQuality Quality = DataQuality.None);

public sealed record NormalizedTrade(TradeEvent Raw, decimal BaseQuantity, decimal QuoteNotional);
public sealed record FxQuote(string FromCurrency, decimal UsdRate, DateTimeOffset TimestampUtc);

public static class TradeNormalizer
{
    public static NormalizedTrade Normalize(TradeEvent t)
    {
        if (t.SchemaVersion != 1) throw new InvalidDataException("Unsupported trade schema version.");
        if (t.Price <= 0 || t.Quantity <= 0 || t.ContractMultiplier <= 0)
            throw new InvalidDataException("Price, quantity and contract multiplier must be positive.");
        if (string.IsNullOrWhiteSpace(t.TradeId) || string.IsNullOrWhiteSpace(t.Source.Identity) ||
            string.IsNullOrWhiteSpace(t.CanonicalSymbol) || string.IsNullOrWhiteSpace(t.BaseAsset) || string.IsNullOrWhiteSpace(t.QuoteAsset) ||
            new[] { t.Source.Venue, t.Source.ConnectionId, t.Source.VenueSymbol }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Trade identity and explicit instrument mapping are required.");
        if (t.ExchangeTimestampUtc.Offset != TimeSpan.Zero || t.ReceiveTimestampUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Trade timestamps must be UTC.");
        if (t.UnderlyingTradeCount is <= 0) throw new InvalidDataException("Known underlying trade count must be positive.");
        if (!Enum.IsDefined(t.Source.MarketType) || !Enum.IsDefined(t.AggressorSide) || !Enum.IsDefined(t.QuantityUnit))
            throw new InvalidDataException("Invalid enum value.");
        if (t.Source.MarketType == MarketType.Spot && t.QuantityUnit != QuantityUnit.Base)
            throw new InvalidDataException("Spot quantities must be base quantities.");
        decimal baseQuantity = t.QuantityUnit == QuantityUnit.Base ? t.Quantity
            : t.Source.MarketType == MarketType.InversePerpetual ? t.Quantity * t.ContractMultiplier / t.Price
            : t.Quantity * t.ContractMultiplier;
        return new(t, baseQuantity, baseQuantity * t.Price);
    }

    public static decimal UsdNotional(NormalizedTrade t, FxQuote? fx, TimeSpan maxAge)
    {
        if (t.Raw.QuoteAsset == "USD") return t.QuoteNotional;
        if (fx is null || fx.FromCurrency != t.Raw.QuoteAsset || fx.UsdRate <= 0 || fx.TimestampUtc.Offset != TimeSpan.Zero ||
            fx.TimestampUtc > t.Raw.ExchangeTimestampUtc || t.Raw.ExchangeTimestampUtc - fx.TimestampUtc > maxAge)
            throw new InvalidDataException("USD normalization requires a matching, causal, fresh FX quote; stablecoins are not assumed equal to USD.");
        return t.QuoteNotional * fx.UsdRate;
    }
}

public sealed record InstrumentMapping(SourceKey Source, string CanonicalSymbol, string BaseAsset, string QuoteAsset,
    QuantityUnit QuantityUnit = QuantityUnit.Base, decimal ContractMultiplier = 1m);
public interface ISymbolMapper { InstrumentMapping Resolve(SourceKey source); }
public sealed class SymbolMapper(IEnumerable<InstrumentMapping> mappings) : ISymbolMapper
{
    private readonly Dictionary<SourceKey, InstrumentMapping> mappings = mappings.ToDictionary(x => x.Source);
    public InstrumentMapping Resolve(SourceKey source) => mappings.TryGetValue(source, out var mapping)
        ? mapping : throw new InvalidDataException($"No explicit mapping for source {source.Identity}.");
}

public interface IMarketDataSource
{
    SourceKey Source { get; }
    IAsyncEnumerable<TradeEvent> ReadAsync(CancellationToken cancellationToken);
}
public interface IExternalExchangeSource : IMarketDataSource { }
public interface INativeQuantowerSource : IMarketDataSource { }
public interface IHistoricalReplaySource : IMarketDataSource { }
public sealed record SourceSelection(string CanonicalSymbol, string BaseAsset, string QuoteAsset, MarketType MarketType, IReadOnlyCollection<SourceKey> Sources)
{
    public bool Accepts(TradeEvent trade) => trade.CanonicalSymbol == CanonicalSymbol && trade.BaseAsset == BaseAsset &&
        trade.QuoteAsset == QuoteAsset && trade.Source.MarketType == MarketType && Sources.Contains(trade.Source);
}

public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)
        ?? throw new InvalidDataException("JSON null is not a valid record.");
    public static decimal Decimal(string value) => decimal.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
}
