namespace VolumeIntelligence.Core;

public sealed class PipelineMetrics
{
    public long Received { get; internal set; }
    public long ExcludedSource { get; internal set; }
    public long Duplicates { get; internal set; }
    public long LateExcluded { get; internal set; }
    public long OutsideSession { get; internal set; }
    public long Applied { get; internal set; }
    public bool IdentityCapacityReached { get; internal set; }
}

public sealed class TradePipeline
{
    private readonly SourceSelection selection;
    private readonly SessionCalendar calendar;
    private readonly ProfileSettings settings;
    private readonly TimeSpan reorderWindow;
    private readonly int maximumIdentities, maximumBufferedEvents;
    private readonly Dictionary<(SourceKey, string), TradeEvent> seen = [];
    private readonly PriorityQueue<NormalizedTrade, EventOrder> queue = new();
    private readonly SortedDictionary<DateTimeOffset, VolumeProfile> profiles = [];
    private DateTimeOffset maxTime = DateTimeOffset.MinValue, finalizedThrough = DateTimeOffset.MinValue;
    private bool finished;
    public PipelineMetrics Metrics { get; } = new();
    public TradePipeline(SourceSelection selection, ProfileSettings settings, SessionSettings? sessions = null,
        TimeSpan? reorderWindow = null, int maximumIdentities = 1_000_000, int maximumBufferedEvents = 100_000)
    {
        if (selection.Sources.Count == 0 || selection.Sources.Count != selection.Sources.Distinct().Count() ||
            selection.Sources.GroupBy(x => (x.Venue, x.MarketType)).Any(x => x.Count() > 1))
            throw new ArgumentException("Select each physical venue/instrument feed once; overlapping native/external routes would double-count trades.");
        this.selection = selection; this.settings = settings; calendar = new(sessions ?? new());
        this.reorderWindow = reorderWindow ?? TimeSpan.FromSeconds(2);
        if (this.reorderWindow < TimeSpan.Zero || maximumIdentities <= 0 || maximumBufferedEvents <= 0)
            throw new ArgumentException("Invalid pipeline limits.");
        this.maximumIdentities = maximumIdentities; this.maximumBufferedEvents = maximumBufferedEvents;
    }

    public void Push(TradeEvent trade)
    {
        if (finished) throw new InvalidOperationException("Pipeline has completed.");
        Metrics.Received++;
        if (!selection.Accepts(trade)) { Metrics.ExcludedSource++; return; }
        var normalized = TradeNormalizer.Normalize(trade);
        var key = (trade.Source, trade.TradeId);
        if (seen.TryGetValue(key, out var prior))
        {
            // Re-delivery may have a different receive time and raw-file reference, but not a different payload.
            if (prior with { ReceiveTimestampUtc = trade.ReceiveTimestampUtc, RawPayloadReference = trade.RawPayloadReference, Quality = trade.Quality } != trade)
                throw new InvalidDataException("Conflicting payload for the same source trade ID.");
            Metrics.Duplicates++; return;
        }
        if (trade.ExchangeTimestampUtc < finalizedThrough) { Metrics.LateExcluded++; return; }
        if (seen.Count >= maximumIdentities)
        {
            Metrics.IdentityCapacityReached = true;
            throw new InvalidDataException("Exact deduplication capacity reached; rotate to a new recording/pipeline. No IDs were silently evicted.");
        }
        if (queue.Count >= maximumBufferedEvents) throw new InvalidDataException("Reorder buffer capacity reached.");
        seen.Add(key, trade);
        maxTime = trade.ExchangeTimestampUtc > maxTime ? trade.ExchangeTimestampUtc : maxTime;
        queue.Enqueue(normalized, new(trade.ExchangeTimestampUtc.UtcTicks, trade.Source.Identity, trade.TradeId));
        var watermark = maxTime - reorderWindow;
        Drain(watermark); finalizedThrough = watermark;
    }

    public IReadOnlyList<ProfileSnapshot> Snapshots() => profiles.Values.Select(x => x.Snapshot()).ToArray();
    public IReadOnlyList<ProfileSnapshot> Complete()
    {
        Drain(DateTimeOffset.MaxValue); finished = true; return Snapshots();
    }

    private void Drain(DateTimeOffset through)
    {
        while (queue.TryPeek(out _, out var priority) && priority.Ticks <= through.UtcTicks)
        {
            var trade = queue.Dequeue(); SessionWindow window;
            try { window = calendar.At(trade.Raw.ExchangeTimestampUtc); }
            catch (OutsideSessionException) { Metrics.OutsideSession++; continue; }
            if (!profiles.TryGetValue(window.StartUtc, out var profile))
                profiles[window.StartUtc] = profile = new(settings, selection, window);
            profile.Add(trade); Metrics.Applied++;
        }
    }

    private readonly record struct EventOrder(long Ticks, string Source, string Id) : IComparable<EventOrder>
    {
        public int CompareTo(EventOrder other)
        {
            int result = Ticks.CompareTo(other.Ticks);
            if (result == 0) result = StringComparer.Ordinal.Compare(Source, other.Source);
            return result == 0 ? StringComparer.Ordinal.Compare(Id, other.Id) : result;
        }
    }
}
