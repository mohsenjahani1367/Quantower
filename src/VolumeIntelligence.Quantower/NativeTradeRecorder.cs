using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using TradingPlatform.BusinessLayer;
using VolumeIntelligence.Core;

namespace VolumeIntelligence.Quantower;

/// <summary>Market-data diagnostic only. No accounts, orders or trading APIs are used.</summary>
public sealed class NativeTradeRecorder : Strategy
{
    [InputParameter("Symbol 1", 0)] public Symbol? Symbol1 { get; set; }
    [InputParameter("Venue 1 (explicit source label)", 1)] public string Venue1 { get; set; } = "Binance";
    [InputParameter("Symbol 2 (optional)", 2)] public Symbol? Symbol2 { get; set; }
    [InputParameter("Venue 2 (explicit source label)", 3)] public string Venue2 { get; set; } = "Bybit";
    [InputParameter("Base asset", 4)] public string BaseAsset { get; set; } = "BTC";
    [InputParameter("Quote asset", 5)] public string QuoteAsset { get; set; } = "USDT";
    [InputParameter("Confirm selected symbols are Spot and Size is base quantity", 6)] public bool ConfirmSpotBaseUnits { get; set; }
    [InputParameter("Output parent directory", 7)] public string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Quantower", "VolumeIntelligence");
    private CaptureRun? current;

    public NativeTradeRecorder()
    {
        Name = "Volume Intelligence - Native Trade Recorder";
        Description = "Read-only public trade capture. Explicit spot/base-unit confirmation required. Unknown IDs and aggressors remain flagged. No trading.";
    }

    protected override void OnRun()
    {
        try
        {
            if (current is not null) throw new InvalidOperationException("Recorder is already running.");
            if (!ConfirmSpotBaseUnits || Symbol1 is null) throw new InvalidOperationException("Select Symbol 1 and verify Spot/base-size semantics first.");
            var mappings = new Dictionary<Symbol, InstrumentMapping>();
            Add(mappings, Symbol1, Venue1); if (Symbol2 is not null) Add(mappings, Symbol2, Venue2);
            string directory = Path.Combine(OutputDirectory, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N"));
            var run = new CaptureRun(mappings, directory);
            current = run;
            run.Start();
            foreach (var symbol in mappings.Keys) symbol.NewLast += run.OnLast;
            Log("Public trade recorder started. Stop after the capture window; inspect capture-status.json before using the data.", StrategyLoggingLevel.Info);
        }
        catch (Exception error) { Log(error.Message, StrategyLoggingLevel.Error); Stop(); }
    }

    private void Add(Dictionary<Symbol, InstrumentMapping> mappings, Symbol symbol, string venue)
    {
        symbol = Core.Instance.GetSymbol(symbol.CreateInfo()) ?? symbol;
        if (!Core.Instance.Connections.Connected.Any(c => c.Id == symbol.ConnectionId))
            throw new InvalidOperationException("Selected symbol's connection is not currently connected.");
        if (string.IsNullOrWhiteSpace(venue) || string.IsNullOrWhiteSpace(BaseAsset) || string.IsNullOrWhiteSpace(QuoteAsset))
            throw new InvalidOperationException("Explicit venue, base and quote mapping is required.");
        if (mappings.Keys.Any(s => s.ConnectionId == symbol.ConnectionId && s.Id == symbol.Id) ||
            mappings.Values.Any(m => m.Source.Venue == venue))
            throw new InvalidOperationException("The same native/physical source cannot be selected twice.");
        var source = new SourceKey(venue, symbol.ConnectionId, MarketType.Spot, symbol.Name);
        mappings.Add(symbol, new(source, BaseAsset + "-" + QuoteAsset, BaseAsset, QuoteAsset));
    }

    protected override void OnStop()
    {
        var run = current; current = null;
        if (run is not null)
        {
            foreach (var symbol in run.Mappings.Keys) symbol.NewLast -= run.OnLast;
            run.Complete();
        }
        base.OnStop();
    }

    private sealed class CaptureRun(Dictionary<Symbol, InstrumentMapping> mappings, string directory)
    {
        public IReadOnlyDictionary<Symbol, InstrumentMapping> Mappings { get; } = mappings;
        private readonly Channel<TradeEvent> queue = Channel.CreateBounded<TradeEvent>(new BoundedChannelOptions(8192)
        { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });
        private long sequence, dropped, rejected, captured;
        private readonly object gate = new();
        private volatile bool stopping;
        private volatile Exception? failure;
        private Task? worker;

        public void Start() => worker = Task.Run(async () =>
        {
            try
            {
                await using var recorder = new JsonLinesRecorder(Path.Combine(directory, "trades.jsonl"));
                await File.WriteAllTextAsync(Path.Combine(directory, "mapping.json"), JsonDefaults.Serialize(Mappings.Values.ToArray()));
                await foreach (var trade in queue.Reader.ReadAllAsync()) { await recorder.RecordAsync(trade); captured++; }
            }
            catch (Exception error) { failure = error; queue.Writer.TryComplete(error); }
            finally
            {
                if (Directory.Exists(directory))
                    await File.WriteAllTextAsync(Path.Combine(directory, "capture-status.json"), JsonDefaults.Serialize(new
                    {
                        schemaVersion = 1, status = failure is not null ? "failed" : captured == 0 ? "no-data" : dropped > 0 || rejected > 0 ? "degraded" : "stopped",
                        captured, dropped = Interlocked.Read(ref dropped), rejected = Interlocked.Read(ref rejected), error = failure?.GetType().Name,
                        warning = "Native aggregation and volume units require provider-specific verification. Missing upstream IDs are synthetic and cannot prove exact upstream deduplication. This capture is not historical backfill."
                    }));
            }
        });

        public void OnLast(Symbol symbol, Last last)
        {
            lock (gate)
            {
            // Callback snapshots only market fields into a bounded queue. No disk/network/UI work.
            try
            {
                if (stopping || failure is not null || !Mappings.TryGetValue(symbol, out var mapping)) return;
                if (!double.IsFinite(last.Price) || !double.IsFinite(last.Size) || last.Price <= 0 || last.Size <= 0 || last.Time.Kind != DateTimeKind.Utc)
                { Interlocked.Increment(ref rejected); return; }
                string? id = last.TradeId?.ToString(); bool missingId = string.IsNullOrWhiteSpace(id);
                var side = last.AggressorFlag switch { AggressorFlag.Buy => AggressorSide.Buy, AggressorFlag.Sell => AggressorSide.Sell, _ => AggressorSide.Unknown };
                var trade = new TradeEvent(1, mapping.Source, mapping.CanonicalSymbol, mapping.BaseAsset, mapping.QuoteAsset,
                    missingId ? "native-event:" + Interlocked.Increment(ref sequence).ToString(CultureInfo.InvariantCulture) : id!,
                    new DateTimeOffset(last.Time), DateTimeOffset.UtcNow, (decimal)last.Price, (decimal)last.Size,
                    QuantityUnit.Base, 1, side, false, null, null, null,
                    DataQuality.NativeAggregationUnverified | (missingId ? DataQuality.NativeIdentityUnverified : DataQuality.None) |
                    (side == AggressorSide.Unknown ? DataQuality.UnknownAggressor : DataQuality.None));
                if (!queue.Writer.TryWrite(trade)) Interlocked.Increment(ref dropped);
            }
            catch { Interlocked.Increment(ref rejected); }
            }
        }
        public void Complete()
        {
            lock (gate)
            {
                stopping = true;
                queue.Writer.TryComplete();
            }
            // Drain asynchronously; restarting creates a separate immutable capture context.
            _ = worker;
        }
    }
}
