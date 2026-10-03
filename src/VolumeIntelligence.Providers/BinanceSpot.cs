using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using VolumeIntelligence.Core;

namespace VolumeIntelligence.Providers;

public static class BinanceAggregateTradeParser
{
    public static TradeEvent Parse(ReadOnlySpan<byte> utf8, InstrumentMapping mapping, DateTimeOffset received,
        string? rawReference = null)
    {
        using var doc = JsonDocument.Parse(utf8.ToArray());
        var root = doc.RootElement;
        if (root.TryGetProperty("data", out var data)) root = data;
        if (root.GetProperty("e").GetString() != "aggTrade") throw new InvalidDataException("Expected a Binance aggregate trade.");
        string symbol = root.GetProperty("s").GetString() ?? throw new InvalidDataException("Missing symbol.");
        if (mapping.Source.Venue != "Binance" || mapping.Source.MarketType != MarketType.Spot ||
            symbol != mapping.Source.VenueSymbol || mapping.QuantityUnit != QuantityUnit.Base)
            throw new InvalidDataException("Binance Spot symbol does not match the explicit mapping.");
        long first = root.GetProperty("f").GetInt64(), last = root.GetProperty("l").GetInt64();
        long id = root.GetProperty("a").GetInt64();
        if (first < 0 || last < first || id < 0) throw new InvalidDataException("Invalid Binance trade ID range.");
        // m=true means the buyer is the maker, therefore the aggressive side is SELL.
        var side = root.GetProperty("m").GetBoolean() ? AggressorSide.Sell : AggressorSide.Buy;
        var result = new TradeEvent(1, mapping.Source, mapping.CanonicalSymbol, mapping.BaseAsset, mapping.QuoteAsset,
            id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("T").GetInt64()), received,
            JsonDefaults.Decimal(root.GetProperty("p").GetString()!), JsonDefaults.Decimal(root.GetProperty("q").GetString()!),
            QuantityUnit.Base, 1, side, true, checked(last - first + 1), id, rawReference, DataQuality.AggregatedTrade);
        TradeNormalizer.Normalize(result); return result;
    }
}

public sealed class BinanceSpotSource : IExternalExchangeSource, ISourceHealthMonitor, IExchangeCapabilityProvider
{
    public static Uri MarketDataEndpoint(string symbol) => new($"wss://data-stream.binance.vision/ws/{symbol.ToLowerInvariant()}@aggTrade");
    private readonly InstrumentMapping mapping;
    private readonly int maximumReconnects;
    private readonly Func<ReadOnlyMemory<byte>, DateTimeOffset, CancellationToken, ValueTask<string?>>? rawRecorder;
    private long messages;
    private int reconnects;
    private long? priorId;
    public SourceKey Source => mapping.Source;
    public SourceHealth Health { get; private set; }
    public event Action<SourceHealth>? HealthChanged;
    public ExchangeCapabilities Capabilities { get; } = new("Binance", MarketType.Spot, "aggTrade", true, false,
        false, false, "Official binance/binance-spot-api-docs web-socket-streams.md; payload fixture tests",
        "Live delivery not established by fixtures. Aggregate message count differs from individual trade count. Reconnect can leave unrecovered gaps; historical backfill is not implemented.");

    public BinanceSpotSource(InstrumentMapping mapping, int maximumReconnects = 5,
        Func<ReadOnlyMemory<byte>, DateTimeOffset, CancellationToken, ValueTask<string?>>? rawRecorder = null)
    {
        if (mapping.Source.Venue != "Binance" || mapping.Source.MarketType != MarketType.Spot ||
            !System.Text.RegularExpressions.Regex.IsMatch(mapping.Source.VenueSymbol, "^[A-Z0-9]{5,30}$") || maximumReconnects < 0)
            throw new ArgumentException("A valid Binance Spot mapping is required.");
        this.mapping = mapping; this.maximumReconnects = maximumReconnects; this.rawRecorder = rawRecorder;
        Health = new(Source, SourceState.Stopped, DateTimeOffset.UtcNow, 0, 0, "Not started.");
    }

    public async IAsyncEnumerable<TradeEvent> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var socket = new ClientWebSocket();
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(15);
                Report(reconnects == 0 ? SourceState.Connecting : SourceState.Reconnecting, "Connecting to the official public market-data endpoint.");
                Exception? failure = null;
                try { await socket.ConnectAsync(MarketDataEndpoint(Source.VenueSymbol), cancellationToken); }
                catch (Exception e) when (e is WebSocketException or HttpRequestException) { failure = e; }
                if (failure is null)
                {
                    Report(SourceState.Connected, reconnects == 0 ? "Connected; no historical backfill." : "Reconnected; gap not backfilled.");
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        byte[]? payload = null;
                        try { payload = await ReceiveMessage(socket, cancellationToken); }
                        catch (Exception e) when (e is WebSocketException or IOException) { failure = e; }
                        if (payload is null) { failure ??= new IOException("Public stream closed."); break; }
                        var receiveTime = DateTimeOffset.UtcNow;
                        string? raw = rawRecorder is null ? null : await rawRecorder(payload, receiveTime, cancellationToken);
                        TradeEvent trade = BinanceAggregateTradeParser.Parse(payload, mapping, receiveTime, raw);
                        messages++;
                        if (priorId is { } id && trade.SourceSequence > id + 1)
                        {
                            trade = trade with { Quality = trade.Quality | DataQuality.GapBefore };
                            Report(SourceState.Gapped, "Aggregate trade ID gap observed; no backfill performed.");
                        }
                        else if (reconnects > 0) trade = trade with { Quality = trade.Quality | DataQuality.GapBefore };
                        priorId = Math.Max(priorId ?? -1, trade.SourceSequence!.Value);
                        yield return trade;
                    }
                }
                if (cancellationToken.IsCancellationRequested) break;
                string diagnosis = DescribeFailure(failure);
                bool accessDenied = diagnosis.Contains("HTTP 401", StringComparison.Ordinal) || diagnosis.Contains("HTTP 403", StringComparison.Ordinal) || diagnosis.Contains("HTTP 451", StringComparison.Ordinal);
                if (accessDenied || reconnects >= maximumReconnects)
                {
                    Report(SourceState.Failed, diagnosis + "; recording is incomplete. " + (accessDenied ? "Access denial is not retried." : "Reconnect budget exhausted."));
                    throw new IOException("Binance public stream failed; inspect the health ledger. No authentication or alternate regional route was attempted.", failure);
                }
                reconnects++;
                Report(SourceState.Reconnecting, diagnosis);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, reconnects - 1))), cancellationToken);
            }
        }
        finally { if (Health.State != SourceState.Failed) Report(SourceState.Stopped, "Stopped; final run status must distinguish timeout, user cancellation, and failure."); }
    }

    private void Report(SourceState state, string detail)
    {
        Health = new(Source, state, DateTimeOffset.UtcNow, messages, reconnects, detail);
        HealthChanged?.Invoke(Health);
    }

    private static string DescribeFailure(Exception? failure)
    {
        // Report status codes/types only, not request headers, credentials or proxy URLs.
        for (Exception? error = failure; error is not null; error = error.InnerException)
        {
            if (error is HttpRequestException http && http.StatusCode is { } code) return $"HTTP {(int)code} ({failure!.GetType().Name})";
            var match = System.Text.RegularExpressions.Regex.Match(error.Message, @"\b(401|403|429|451|5\d\d)\b");
            if (match.Success) return "HTTP " + match.Value + " (" + failure!.GetType().Name + ")";
        }
        return failure?.GetType().Name ?? "Disconnected";
    }

    private static async Task<byte[]?> ReceiveMessage(ClientWebSocket socket, CancellationToken token)
    {
        byte[] buffer = new byte[8192]; using var message = new MemoryStream();
        while (true)
        {
            var received = await socket.ReceiveAsync(buffer.AsMemory(), token);
            if (received.MessageType == WebSocketMessageType.Close) return null;
            if (received.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Expected a text market-data message.");
            if (message.Length + received.Count > 1_048_576) throw new InvalidDataException("WebSocket message exceeds 1 MiB.");
            message.Write(buffer, 0, received.Count);
            if (received.EndOfMessage) return message.ToArray();
        }
    }
}
