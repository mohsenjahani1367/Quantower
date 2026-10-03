# Data dictionary

Trade JSONL schema version 1: one JSON object per line. Blank, malformed and unsupported records fail replay.

| Field | Meaning |
|---|---|
| source | venue, connectionId, marketType, venueSymbol; case-sensitive identity |
| canonicalSymbol / baseAsset / quoteAsset | explicit mapping |
| tradeId | source-scoped ID; native fallback IDs carry a flag |
| exchangeTimestampUtc | event time for session/order calculations |
| receiveTimestampUtc | arrival time for replay pacing/latency inspection |
| price / quantity | positive decimals; original quantity unit retained |
| quantityUnit / contractMultiplier | Base or Contracts; linear multiplier in base, inverse multiplier in quote |
| aggressorSide | Buy/Sell/Unknown; Binance buyer-maker=true means Sell |
| isAggregatedTrade | true for Binance aggTrade; native aggregation is flagged unverified |
| underlyingTradeCount | nullable; Binance last-first+1; native event count is not presumed individual trade count |
| sourceSequence | Binance aggregate ID; nullable elsewhere |
| rawPayloadReference | raw JSONL filename/line; public payload only |
| quality | UnknownAggressor, AggregatedTrade, NativeIdentityUnverified, GapBefore, NativeAggregationUnverified |

QuoteNotional=BaseQuantity×price in exact quote currency. Inverse prices are quote/base and require the correct multiplier. Core FX requires causal conversion with max age five minutes; CLI FX-history injection is deferred.

Profiles contain total/buy/sell/unknown volume, known-side delta, event and nullable underlying counts, ordered occupied rows, source contributions, quality flags and session boundaries. Unknown volume contributes to Total/VA, not directional delta. Current GUI source draws levels only.
