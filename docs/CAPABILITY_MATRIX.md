# Capability matrix

API presence is not data delivery. Offline tests, user-machine observations and unverified live capabilities remain distinct.

| Source | Trades/aggressor | History | L2 | OI/funding/liquidations | Account key | Evidence |
|---|---|---|---|---|---|---|
| External Binance Spot | WS/aggTrade parser; maker mapping tested | no backfill implemented | not implemented | not applicable/not implemented | not required | official schema and synthetic fixtures; live cloud egress blocked |
| Native Quantower Binance Spot | NewLast recorder source; units/IDs await test | six actual cached bars supplied; tick history untested | API exists; payload unverified | unverified | existing public connection; Bridge token local | user evidence on 1.146.18.0; actual adapter build pending |
| Binance USD-M / Hyperliquid | not implemented | unverified | not implemented | not implemented | public-data design | roadmap |
| Bybit / OKX / others | not implemented | unverified | not implemented | not implemented | INFO/public access to verify | roadmap |

Official Binance WS docs describe 24-hour connections and server ping every 20 seconds. Client uses .NET ping/pong support and bounded reconnection; disconnected intervals are flagged, not represented as complete data. No depth snapshot/rebuild claim.

Native recorder requires explicit Spot/base-size confirmation and rejects invalid prices/sizes/non-UTC timestamps. Actual units, payload, reconnect and trial remain to validate.

Sources: https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md and https://github.com/Quantower/Examples (inspected commit ae515c3681006dbda8d77a4b99eb3b7465b9787d). Bridge evidence ZIP SHA-256: 4E2B90683B86A2F63E968565661EB0ABF53B775282BD84C148FC7AB3CEB6801E. Current INFO modes and license rules were not independently verified in the GUI.
