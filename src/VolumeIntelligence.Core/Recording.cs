using System.Runtime.CompilerServices;
using System.Text;

namespace VolumeIntelligence.Core;

public interface IMarketDataRecorder : IAsyncDisposable
{
    ValueTask RecordAsync(TradeEvent trade, CancellationToken cancellationToken = default);
}
public sealed class JsonLinesRecorder : IMarketDataRecorder
{
    private readonly StreamWriter writer;
    public JsonLinesRecorder(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        writer = new(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
    }
    public async ValueTask RecordAsync(TradeEvent trade, CancellationToken cancellationToken = default)
    {
        TradeNormalizer.Normalize(trade);
        await writer.WriteLineAsync(JsonDefaults.Serialize(trade).AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }
    public ValueTask DisposeAsync() => writer.DisposeAsync();
}

public sealed class JsonLinesReplay(string path, SourceKey source, double speed = 0) : IHistoricalReplaySource
{
    public SourceKey Source { get; } = source;
    public async IAsyncEnumerable<TradeEvent> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(speed) || speed < 0) throw new ArgumentException("Replay speed must be finite and nonnegative.");
        using var reader = new StreamReader(path);
        DateTimeOffset? prior = null; int lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) throw new InvalidDataException($"Blank replay record at line {lineNumber}.");
            TradeEvent trade;
            try { trade = JsonDefaults.Deserialize<TradeEvent>(line); TradeNormalizer.Normalize(trade); }
            catch (Exception e) when (e is System.Text.Json.JsonException or InvalidDataException or ArgumentException)
            { throw new InvalidDataException($"Invalid replay record at line {lineNumber}.", e); }
            if (speed > 0 && prior is { } stamp)
            {
                var delay = (trade.ReceiveTimestampUtc - stamp).TotalMilliseconds / speed;
                if (delay > 0) await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delay, int.MaxValue - 1)), cancellationToken);
            }
            prior = trade.ReceiveTimestampUtc;
            yield return trade;
        }
    }
}
