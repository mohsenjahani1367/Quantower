# Known limitations

- First independent slice only; no complete suite or profitability claim. Existing Pinbar is unchanged and not integrated.
- Cloud cannot reach desktop loopback. Bridge still exposes status/connections only.
- Quantower DLL is absent. Native/indicator sources are outside cloud solution; not claimed compiled or runtime-tested.
- Live Binance delivery/reconnect require permitted network. Synthetic parser fixtures cannot validate access/latency.
- One external Spot provider. Futures/Hyperliquid, daemon/custom connector, L2, derivatives and later indicators are deferred.
- Native source allows up to two explicitly mapped Spot symbols with confirmed base-size units. No inferred market, units, count or IDs; unverified values flagged. Native connection health/backfill are deferred.
- Native drops/rejects are counted; wait for capture-status.json after stopping. Abrupt process kill may leave partial data without final metadata.
- Live reconnect gaps are not recovered; capture remains degraded. No regional bypass is attempted.
- Overlapping physical feeds are rejected; cross-route deduplication is not assumed.
- Daily, Weekly/Monday, Monthly and custom daily/overnight sessions exist. Dedicated regional presets, intraday grids and holidays are deferred.
- Late policy is Exclude with configurable reorder window; Include/Correct/revisions deferred. Replay preserves original arrival order.
- Strict quote/market baskets. Core explicit FX conversion exists; CLI FX histories, venue weights and reference-price conversion deferred.
- Limits: one million IDs, 100,000 reorder events; profiles retained for all sessions. Rotate bounded datasets and use an appropriate row size.
- GUI source shows latest fully closed session levels on chart updates. Histogram, developing profiles, historical redraw, naked extensions and GUI health display are deferred.
- Engine has no chart timeframe input; actual multi-timeframe Quantower comparison still requires local screenshots.
- PowerShell scripts prepared but not executed on Linux; real Windows build logs are required before installation.
