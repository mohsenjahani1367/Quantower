using System.Text;
using VolumeIntelligence.Core;
using VolumeIntelligence.Providers;

var tests = new List<(string Name, Func<Task> Run)>();
void Test(string name, Action run) => tests.Add((name, () => { run(); return Task.CompletedTask; }));
void AsyncTest(string name, Func<Task> run) => tests.Add((name, run));
void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}."); }
void True(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
void Throws<T>(Action run) where T : Exception
{ try { run(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }

var source = new SourceKey("Binance", "binance-public-spot", MarketType.Spot, "BTCUSDT");
var mapping = new InstrumentMapping(source, "BTC-USDT", "BTC", "USDT");
var selection = new SourceSelection("BTC-USDT", "BTC", "USDT", MarketType.Spot, [source]);
DateTimeOffset epoch = DateTimeOffset.Parse("2026-10-03T12:00:00Z");
TradeEvent Trade(string id, decimal price = 100, decimal size = 1, AggressorSide side = AggressorSide.Buy, double second = 0) =>
    new(1, source, "BTC-USDT", "BTC", "USDT", id, epoch.AddSeconds(second), epoch.AddSeconds(second + .1), price,
        size, QuantityUnit.Base, 1, side, false, 1, null, null);
TradeEvent[] Golden() => [Trade("1", 100, 3, second: 0), Trade("2", 101, 4, AggressorSide.Sell, 1), Trade("3", 102, 3, second: 2)];
ProfileSnapshot Calculate(IEnumerable<TradeEvent> trades, ProfileSettings? settings = null)
{ var p = new TradePipeline(selection, settings ?? new(1, .7m, VolumeUnit.Base)); foreach (var t in trades) p.Push(t); return p.Complete().Single(); }

Test("Hand-calculated POC, contiguous VA, buy/sell volume and delta", () =>
{
    var p = Calculate(Golden()); Equal(10m, p.TotalVolume); Equal(6m, p.BuyVolume); Equal(4m, p.SellVolume);
    Equal(2m, p.Delta); Equal<decimal?>(101m, p.Poc); Equal<decimal?>(100m, p.Val); Equal<decimal?>(102m, p.Vah);
    Equal(7m, p.ValueAreaVolume); Equal(3L, p.EventCount); Equal<long?>(3, p.UnderlyingTradeCount);
});
Test("POC tie chooses lowest price", () => Equal<decimal?>(100, Calculate([Trade("1", 101), Trade("2", 100)]).Poc));
Test("VA adjacent volume tie expands down", () => Equal<decimal?>(100, Calculate([Trade("1", 100, 3), Trade("2", 101, 4), Trade("3", 102, 3)]).Val));
Test("Price bins use decimal floor, not nearest rounding", () => Equal<decimal?>(100, Calculate([Trade("1", 100.99m)]).Poc));
Test("Unknown aggressor remains visible and excluded from directional delta", () =>
{ var p = Calculate([Trade("1", side: AggressorSide.Unknown) with { UnderlyingTradeCount = null }]); Equal(1m, p.UnknownVolume); Equal(0m, p.Delta); Equal<long?>(null, p.UnderlyingTradeCount); True(p.Quality.HasFlag(DataQuality.UnknownAggressor)); });
Test("Quote notional is distinct from base volume", () => Equal(1010m, Calculate(Golden(), new(1, .7m, VolumeUnit.QuoteNotional)).TotalVolume));
Test("Spot normalization", () => { var n = TradeNormalizer.Normalize(Trade("1", 100, 3)); Equal(3m, n.BaseQuantity); Equal(300m, n.QuoteNotional); });
Test("Linear contracts use contract multiplier", () =>
{ var n = TradeNormalizer.Normalize(Trade("1", 100, 3) with { Source = source with { MarketType = MarketType.LinearPerpetual }, QuantityUnit = QuantityUnit.Contracts, ContractMultiplier = .1m }); Equal(.3m, n.BaseQuantity); Equal(30m, n.QuoteNotional); });
Test("Inverse contracts convert quote contract value to base", () =>
{ var n = TradeNormalizer.Normalize(Trade("1", 100, 3) with { Source = source with { MarketType = MarketType.InversePerpetual }, QuantityUnit = QuantityUnit.Contracts, ContractMultiplier = 10 }); Equal(.3m, n.BaseQuantity); Equal(30m, n.QuoteNotional); });
Test("USDT cannot silently become USD", () => Throws<InvalidDataException>(() => TradeNormalizer.UsdNotional(TradeNormalizer.Normalize(Trade("1")), null, TimeSpan.FromMinutes(5))));
Test("Causal explicit FX conversion", () => Equal(99m, TradeNormalizer.UsdNotional(TradeNormalizer.Normalize(Trade("1")), new("USDT", .99m, epoch), TimeSpan.FromMinutes(5))));
Test("Future and stale FX quotes rejected", () =>
{ var n = TradeNormalizer.Normalize(Trade("1")); Throws<InvalidDataException>(() => TradeNormalizer.UsdNotional(n, new("USDT", 1, epoch.AddSeconds(1)), TimeSpan.FromMinutes(5))); Throws<InvalidDataException>(() => TradeNormalizer.UsdNotional(n, new("USDT", 1, epoch.AddMinutes(-6)), TimeSpan.FromMinutes(5))); });
Test("Strict quote, canonical instrument, market and selected source exclusion", () =>
{
    var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base));
    p.Push(Trade("1") with { QuoteAsset = "USDC" }); p.Push(Trade("2") with { CanonicalSymbol = "ETH-USDT" });
    p.Push(Trade("3") with { Source = source with { MarketType = MarketType.LinearPerpetual } });
    p.Push(Trade("4") with { Source = source with { ConnectionId = "not-selected" } });
    Equal(0, p.Complete().Count); Equal(4L, p.Metrics.ExcludedSource);
});
Test("Custom two-venue basket and source contributions", () =>
{
    var other = source with { Venue = "FixtureVenue", ConnectionId = "fixture" };
    var p = new TradePipeline(selection with { Sources = [source, other] }, new(1, .7m, VolumeUnit.Base));
    p.Push(Trade("1")); p.Push(Trade("1") with { Source = other });
    var s = p.Complete().Single(); Equal(2m, s.TotalVolume); Equal(2, s.SourceContributions.Count);
});
Test("Overlapping native/external routes for the same physical feed rejected", () =>
{
    Throws<ArgumentException>(() => new TradePipeline(selection with { Sources = [source, source with { ConnectionId = "native" }] }, new()));
    Throws<ArgumentException>(() => new TradePipeline(selection with { Sources = [source, source with { ConnectionId = "native", VenueSymbol = "BTC/USDT" }] }, new()));
});
Test("Duplicate redelivery changes receive time but does not change volume", () =>
{
    var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base)); p.Push(Trade("1"));
    p.Push(Trade("1") with { ReceiveTimestampUtc = epoch.AddSeconds(1), RawPayloadReference = "second-delivery" });
    Equal(1m, p.Complete().Single().TotalVolume); Equal(1L, p.Metrics.Duplicates);
});
Test("Same trade ID with a conflicting payload fails", () =>
{ var p = new TradePipeline(selection, new()); p.Push(Trade("1")); Throws<InvalidDataException>(() => p.Push(Trade("1", size: 2))); });
Test("Out-of-order events within reorder window preserve profile", () =>
    Equal(JsonDefaults.Serialize(Calculate(Golden())), JsonDefaults.Serialize(Calculate(Golden().Reverse()))));
Test("Late event outside watermark is explicitly counted and excluded", () =>
{
    var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base)); p.Push(Trade("1", second: 10)); p.Push(Trade("2", second: 1));
    Equal(1m, p.Complete().Single().TotalVolume); Equal(1L, p.Metrics.LateExcluded);
});
Test("Equal timestamps accepted with zero reorder window", () =>
{ var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base), reorderWindow: TimeSpan.Zero); p.Push(Trade("1")); p.Push(Trade("2")); Equal(2m, p.Complete().Single().TotalVolume); });
Test("Dedup identity limit fails without silently evicting IDs", () =>
{ var p = new TradePipeline(selection, new(), maximumIdentities: 1); p.Push(Trade("1")); Throws<InvalidDataException>(() => p.Push(Trade("2"))); True(p.Metrics.IdentityCapacityReached); });
Test("Reorder queue limit is enforced", () =>
{ var p = new TradePipeline(selection, new(), maximumBufferedEvents: 1); p.Push(Trade("1")); Throws<InvalidDataException>(() => p.Push(Trade("2"))); });
Test("Price-span guard prevents runaway sparse-bin allocation", () =>
{ var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base, 10)); p.Push(Trade("1", 1)); Throws<InvalidDataException>(() => { p.Push(Trade("2", 100)); p.Complete(); }); });
Test("UTC daily boundary splits sessions", () =>
{ var p = new TradePipeline(selection, new()); p.Push(Trade("1") with { ExchangeTimestampUtc = DateTimeOffset.Parse("2026-10-03T23:59:59Z") }); p.Push(Trade("2") with { ExchangeTimestampUtc = DateTimeOffset.Parse("2026-10-04T00:00:00Z") }); Equal(2, p.Complete().Count); });
Test("Weekly Monday and monthly boundaries", () =>
{ var w = new SessionCalendar(new(SessionKind.Weekly)).At(epoch); Equal(DateTimeOffset.Parse("2026-09-28T00:00:00Z"), w.StartUtc); var m = new SessionCalendar(new(SessionKind.Monthly)).At(epoch); Equal(DateTimeOffset.Parse("2026-10-01T00:00:00Z"), m.StartUtc); });
Test("New York spring and fall DST session lengths", () =>
{ var c = new SessionCalendar(new(TimeZoneId: "America/New_York")); var a = c.At(DateTimeOffset.Parse("2026-03-08T12:00:00Z")); var b = c.At(DateTimeOffset.Parse("2026-11-01T12:00:00Z")); Equal(23d, (a.EndUtc-a.StartUtc).TotalHours); Equal(25d, (b.EndUtc-b.StartUtc).TotalHours); });
Test("DST ambiguous boundary chooses first occurrence with UTC membership", () =>
{ var w = new SessionCalendar(new(TimeZoneId: "America/New_York", Start: "01:30")).At(DateTimeOffset.Parse("2026-11-01T06:15:00Z")); Equal(DateTimeOffset.Parse("2026-11-01T05:30:00Z"), w.StartUtc); });
Test("DST nonexistent boundary advances to first valid minute", () =>
{ var w = new SessionCalendar(new(TimeZoneId: "America/New_York", Start: "02:30")).At(DateTimeOffset.Parse("2026-03-08T12:00:00Z")); Equal(DateTimeOffset.Parse("2026-03-08T07:00:00Z"), w.StartUtc); });
Test("Overnight custom session and out-of-session rejection", () =>
{ var c = new SessionCalendar(new(SessionKind.CustomDaily, "UTC", "22:00", "02:00")); var w = c.At(DateTimeOffset.Parse("2026-10-04T01:00:00Z")); Equal(DateTimeOffset.Parse("2026-10-03T22:00:00Z"), w.StartUtc); Throws<OutsideSessionException>(() => c.At(epoch)); });
Test("Snapshot frequency does not mutate or change the trade profile", () =>
{
    string baseline = JsonDefaults.Serialize(Calculate(Golden()));
    foreach (int interval in new[] { 1, 2, 3 })
    {
        var p = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base)); int count = 0;
        foreach (var t in Golden()) { p.Push(t); if (++count % interval == 0) p.Snapshots(); }
        Equal(baseline, JsonDefaults.Serialize(p.Complete().Single()));
    }
});
Test("Binance maker flag and individual trade count", () =>
{
    var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures/binance-aggtrade.json"));
    var t = BinanceAggregateTradeParser.Parse(bytes, mapping, epoch);
    Equal(AggressorSide.Sell, t.AggressorSide); Equal<long?>(3, t.UnderlyingTradeCount); Equal(100m, t.Price); Equal(3m, t.Quantity);
    var buy = BinanceAggregateTradeParser.Parse(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"m\":true", "\"m\":false")), mapping, epoch);
    Equal(AggressorSide.Buy, buy.AggressorSide);
});
Test("Binance combined-stream envelope", () =>
{ string raw = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/binance-aggtrade.json")); var t = BinanceAggregateTradeParser.Parse(Encoding.UTF8.GetBytes("{\"stream\":\"btcusdt@aggTrade\",\"data\":"+raw+"}"), mapping, epoch); Equal("900", t.TradeId); });
Test("Binance wrong symbol and corrupt ID range rejected", () =>
{ string raw = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/binance-aggtrade.json")); Throws<InvalidDataException>(() => BinanceAggregateTradeParser.Parse(Encoding.UTF8.GetBytes(raw.Replace("BTCUSDT", "ETHUSDT")), mapping, epoch)); Throws<InvalidDataException>(() => BinanceAggregateTradeParser.Parse(Encoding.UTF8.GetBytes(raw.Replace("\"l\":1002", "\"l\":999")), mapping, epoch)); });
Test("Zero price, negative quantity, non-UTC and unsupported schema rejected", () =>
{ foreach (var t in new[] { Trade("1", 0), Trade("1", size: -1), Trade("1") with { SchemaVersion = 2 }, Trade("1") with { ExchangeTimestampUtc = epoch.ToOffset(TimeSpan.FromHours(1)) } }) Throws<InvalidDataException>(() => TradeNormalizer.Normalize(t)); });
AsyncTest("Recorder and replay roundtrip is deterministic and does not overwrite", async () =>
{
    string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".jsonl");
    try
    {
        await using (var r = new JsonLinesRecorder(path)) foreach (var t in Golden()) await r.RecordAsync(t);
        Throws<IOException>(() => new JsonLinesRecorder(path));
        var first = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base)); var second = new TradePipeline(selection, new(1, .7m, VolumeUnit.Base));
        await foreach (var t in new JsonLinesReplay(path, source).ReadAsync()) first.Push(t);
        await foreach (var t in new JsonLinesReplay(path, source).ReadAsync()) second.Push(t);
        Equal(JsonDefaults.Serialize(first.Complete()), JsonDefaults.Serialize(second.Complete())); Equal(3L, first.Metrics.Applied);
    }
    finally { File.Delete(path); }
});
AsyncTest("Replay refuses malformed schema rather than skipping it", async () =>
{
    string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".jsonl");
    try { await File.WriteAllTextAsync(path, "{}\n"); bool threw=false; try { await foreach (var _ in new JsonLinesReplay(path,source).ReadAsync()) {} } catch(InvalidDataException) { threw=true; } True(threw); }
    finally { File.Delete(path); }
});

int passed=0, failed=0;
foreach (var (name, run) in tests)
{
    try { await run(); passed++; Console.WriteLine("PASS "+name); }
    catch(Exception e) { failed++; Console.Error.WriteLine("FAIL "+name+": "+e.Message); }
}
Console.WriteLine($"RESULT executed={tests.Count} passed={passed} failed={failed} skipped=0");
return failed == 0 ? 0 : 1;
