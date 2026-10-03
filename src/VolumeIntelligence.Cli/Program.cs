using System.Globalization;
using System.Text;
using VolumeIntelligence.Core;
using VolumeIntelligence.Providers;

return await Run(args);

static async Task<int> Run(string[] args)
{
    if (args.Length < 2 || args[0] is not ("replay" or "record"))
    {
        Console.Error.WriteLine("Usage: replay <trades.jsonl> --config <settings.json> [--output <report.json>] [--speed 0]\n       record <new-recording-directory> --config <settings.json> [--seconds 60] [--max-reconnects 5]\nNo API keys are accepted. Live connections require explicit 'record'.");
        return 2;
    }
    try
    {
        var options = ParseOptions(args.Skip(2).ToArray());
        if (!options.TryGetValue("config", out string? configPath)) throw new ArgumentException("--config is required.");
        var config = JsonDefaults.Deserialize<RunSettings>(await File.ReadAllTextAsync(configPath));
        Validate(config);
        var pipeline = new TradePipeline(config.Selection, config.Profile, config.Session, TimeSpan.FromMilliseconds(config.ReorderWindowMilliseconds));
        if (args[0] == "replay")
        {
            double speed = options.TryGetValue("speed", out var value) ? double.Parse(value, CultureInfo.InvariantCulture) : 0;
            var replay = new JsonLinesReplay(args[1], config.Selection.Sources.First(), speed);
            await foreach (var trade in replay.ReadAsync()) pipeline.Push(trade);
            var replayProfiles = pipeline.Complete();
            bool replayDegraded = pipeline.Metrics.LateExcluded > 0 || replayProfiles.Any(p => p.Quality.HasFlag(DataQuality.GapBefore));
            string replayStatus = pipeline.Metrics.Applied == 0 ? "no-data" : replayDegraded ? "degraded" : "replayed";
            var result = new { schemaVersion = 1, status = replayStatus, profiles = replayProfiles, metrics = pipeline.Metrics };
            string report = JsonDefaults.Serialize(result);
            if (options.TryGetValue("output", out var output))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                await using var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
                await file.WriteAsync(Encoding.UTF8.GetBytes(report));
            }
            else Console.WriteLine(report);
            return pipeline.Metrics.Applied == 0 ? 4 : replayDegraded ? 3 : 0;
        }

        if (config.Selection.Sources.Count != 1) throw new ArgumentException("This first live slice records one explicit Binance Spot source; multi-source replay is supported.");
        SourceKey sourceKey = config.Selection.Sources.Single();
        var mapper = new SymbolMapper(config.Mappings);
        var mapping = mapper.Resolve(sourceKey);
        if (Directory.Exists(args[1])) throw new IOException("Recording directory already exists; use a new directory.");
        int seconds = options.TryGetValue("seconds", out var secondsText) ? int.Parse(secondsText, CultureInfo.InvariantCulture) : 60;
        int maxReconnects = options.TryGetValue("max-reconnects", out var retriesText) ? int.Parse(retriesText, CultureInfo.InvariantCulture) : 5;
        if (seconds <= 0 || seconds > 86_400) throw new ArgumentException("--seconds must be between 1 and 86400.");
        Directory.CreateDirectory(args[1]);
        await File.WriteAllTextAsync(Path.Combine(args[1], "settings.json"), JsonDefaults.Serialize(config));
        using var stop = new CancellationTokenSource();
        using var duration = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, duration.Token);
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        await using var recorder = new JsonLinesRecorder(Path.Combine(args[1], "trades.jsonl"));
        await using var raw = new StreamWriter(Path.Combine(args[1], "raw.jsonl"), false, new UTF8Encoding(false));
        using var health = new StreamWriter(Path.Combine(args[1], "health.jsonl"), false, new UTF8Encoding(false));
        long rawLine = 0; int statusCode = 0; string status = "completed"; string? error = null;
        var source = new BinanceSpotSource(mapping, maxReconnects, async (payload, received, token) =>
        {
            rawLine++;
            await raw.WriteLineAsync(JsonDefaults.Serialize(new { schemaVersion = 1, receiveTimestampUtc = received, payload = Encoding.UTF8.GetString(payload.Span) }).AsMemory(), token);
            await raw.FlushAsync(token); return $"raw.jsonl:{rawLine}";
        });
        source.HealthChanged += h => { health.WriteLine(JsonDefaults.Serialize(h)); health.Flush(); };
        try
        {
            await foreach (var trade in source.ReadAsync(linked.Token))
            {
                // Record delivery order before deduplication, so replay reproduces all decisions.
                await recorder.RecordAsync(trade, CancellationToken.None); pipeline.Push(trade);
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        { status = stop.IsCancellationRequested ? "cancelled" : "completed-duration"; statusCode = stop.IsCancellationRequested ? 130 : 0; }
        catch (Exception e) { status = "failed"; statusCode = 1; error = e.Message; }
        finally { Console.CancelKeyPress -= cancel; }
        var profiles = pipeline.Complete();
        if (pipeline.Metrics.Applied == 0 && statusCode == 0) { status = "no-data"; statusCode = 4; }
        if (statusCode == 0 && (pipeline.Metrics.LateExcluded > 0 || source.Health.Reconnects > 0 || profiles.Any(p => p.Quality.HasFlag(DataQuality.GapBefore))))
        { status = "degraded"; statusCode = 3; }
        var reportObject = new { schemaVersion = 1, status, error, durationSeconds = seconds, sourceHealth = source.Health, profiles, metrics = pipeline.Metrics };
        await File.WriteAllTextAsync(Path.Combine(args[1], "report.json"), JsonDefaults.Serialize(reportObject));
        Console.WriteLine(JsonDefaults.Serialize(reportObject)); return statusCode;
    }
    catch (Exception e) { Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}"); return 1; }
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    for (int i = 0; i < args.Length; i += 2)
    {
        if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Options require --name value pairs.");
        string key = args[i][2..];
        if (key is not ("config" or "output" or "speed" or "seconds" or "max-reconnects") || !result.TryAdd(key, args[i + 1]))
            throw new ArgumentException("Unknown or duplicate option.");
    }
    return result;
}

static void Validate(RunSettings c)
{
    if (c.SchemaVersion != 1 || c.Selection.Sources.Count == 0 || c.Selection.Sources.Distinct().Count() != c.Selection.Sources.Count)
        throw new InvalidDataException("Invalid configuration version or source selection.");
    var mapper = new SymbolMapper(c.Mappings);
    foreach (var source in c.Selection.Sources)
    {
        var m = mapper.Resolve(source);
        if (m.CanonicalSymbol != c.Selection.CanonicalSymbol || m.BaseAsset != c.Selection.BaseAsset || m.QuoteAsset != c.Selection.QuoteAsset || source.MarketType != c.Selection.MarketType)
            throw new InvalidDataException("Selected source mapping does not match the instrument/market/quote basket.");
    }
}

public sealed record RunSettings(int SchemaVersion, SourceSelection Selection, InstrumentMapping[] Mappings,
    ProfileSettings Profile, SessionSettings Session, int ReorderWindowMilliseconds = 2000);
