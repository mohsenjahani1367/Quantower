using System.Drawing;
using TradingPlatform.BusinessLayer;
using VolumeIntelligence.Core;

namespace VolumeIntelligence.Quantower;

/// <summary>Loads a trade replay off the UI thread; plots its session levels without chart-timeframe-dependent volume allocation.</summary>
public sealed class ReplayVpsvIndicator : Indicator
{
    [InputParameter("Trade JSONL path", 0)] public string TradeFile { get; set; } = "";
    [InputParameter("Pipeline settings JSON path", 1)] public string SettingsFile { get; set; } = "";
    private CancellationTokenSource? stop;
    private volatile IReadOnlyList<ProfileSnapshot> profiles = Array.Empty<ProfileSnapshot>();
    public string LoadStatus { get; private set; } = "Not loaded";
    public ReplayVpsvIndicator()
    {
        Name = "Volume Intelligence - VPSV Replay Levels";
        Description = "Trade-based completed-session POC/VAH/VAL. Replay only; use the recorder to create data. Requires chart-symbol match. No orders.";
        SeparateWindow = false;
        AddLineSeries("POC", Color.Gold, 1, LineStyle.Solid);
        AddLineSeries("VAH", Color.LimeGreen, 1, LineStyle.Solid);
        AddLineSeries("VAL", Color.OrangeRed, 1, LineStyle.Solid);
    }

    protected override void OnInit()
    {
        stop?.Cancel(); stop = new(); var token = stop.Token;
        profiles = Array.Empty<ProfileSnapshot>(); LoadStatus = "Loading";
        _ = Task.Run(async () =>
        {
            try
            {
                var config = JsonDefaults.Deserialize<ReplaySettings>(await File.ReadAllTextAsync(SettingsFile, token));
                // Explicit mapping prevents displaying another instrument's levels on this chart.
                if (!config.Selection.Sources.Any(s => s.ConnectionId == Symbol.ConnectionId && s.VenueSymbol == Symbol.Name) &&
                    !config.Mappings.Any(m => m.Source.ConnectionId == Symbol.ConnectionId && m.Source.VenueSymbol == Symbol.Name && m.CanonicalSymbol == config.Selection.CanonicalSymbol))
                    throw new InvalidDataException("Chart symbol is not explicitly mapped in settings.");
                var pipeline = new TradePipeline(config.Selection, config.Profile, config.Session, TimeSpan.FromMilliseconds(config.ReorderWindowMilliseconds));
                await foreach (var trade in new JsonLinesReplay(TradeFile, config.Selection.Sources.First()).ReadAsync(token)) pipeline.Push(trade);
                var loaded = pipeline.Complete();
                if (!token.IsCancellationRequested) { profiles = loaded; LoadStatus = "Loaded " + loaded.Count + " sessions; late excluded=" + pipeline.Metrics.LateExcluded; }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error) { if (!token.IsCancellationRequested) LoadStatus = "Failed: " + error.Message; }
        }, token);
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        DateTime stamp = Time();
        if (stamp.Kind != DateTimeKind.Utc) { LoadStatus = "Chart timestamps must be verified as UTC"; return; }
        // Show only the latest fully CLOSED session. A completed profile is never projected backwards into its own session.
        var profile = profiles.LastOrDefault(p => p.Session.EndUtc <= new DateTimeOffset(stamp));
        SetValue(profile?.Poc is { } poc ? (double)poc : double.NaN, 0);
        SetValue(profile?.Vah is { } vah ? (double)vah : double.NaN, 1);
        SetValue(profile?.Val is { } val ? (double)val : double.NaN, 2);
    }

    protected override void OnClear()
    {
        stop?.Cancel(); stop?.Dispose(); stop = null;
        profiles = Array.Empty<ProfileSnapshot>(); base.OnClear();
    }
    private sealed record ReplaySettings(int SchemaVersion, SourceSelection Selection, InstrumentMapping[] Mappings,
        ProfileSettings Profile, SessionSettings Session, int ReorderWindowMilliseconds = 2000);
}
