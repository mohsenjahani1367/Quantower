# Validation — 2026-10-03

## Current cloud evidence

| Check | Actual outcome |
|---|---|
| Official SDK artifact SHA-512 | matched official metadata; SDK 10.0.401 |
| `bash scripts/bootstrap-cloud.sh` | installed local SDK and Release build succeeded |
| `bash scripts/verify.sh` | Release build, executable suite, golden CLI replay and byte comparison passed |
| Executed tests | 36 passed, 0 failed, 0 skipped |
| Golden fixture | four deliveries, three unique events, one duplicate; Total 10, Delta 2, POC 101, VAL 100, VAH 102 |
| Replay twice | byte-identical output |
| Empty replay | exit 4 / no-data |
| Malformed replay | exit 1 |
| Replay carrying GapBefore | must return degraded exit 3; tested by CLI negative check |
| Public live Binance probe | FAILED: HTTP 403 (WebSocketException), 0 messages, exit 1; no retry on access denial |
| Native/indicator real API build | UNRUN: DLL exists only on user's Windows machine |
| Native public payload and chart display | UNRUN: Windows smoke evidence required |
| Synthetic whole-process CLI replay benchmark | 20,000 events, 0.5732 seconds, about 34,892 events/s, peak child RSS 72,412 KiB; applied count and total volume checked |

Run evidence is kept under ignored `artifacts/validation/` and included in the handoff ZIP. Synthetic fixtures are explicitly labelled; no sample is presented as real exchange delivery. The provided native-cache sample establishes Bar History only, not trade-level profiling.

The suite checks hand-calculated POC/VA/Delta, tie-breaking, source contributions/selection, contract normalization, causal FX, exact duplicates/conflicts, reorder/late policies, capacities, UTC/weekly/monthly/overnight/DST sessions, parser maker flags/ID ranges, malformed data and recorder/replay. Snapshot cadence is tested independently of profile arithmetic. Actual GUI timeframe parity is still unrun.

No `dotnet test` zero-test outcome is used. The assertion executable prints every named outcome and exits nonzero on failure. CLI exit codes: 0 completed nondegraded data, 1 failure, 2 usage error, 3 degraded/late/gapped, 4 no-data, 130 user cancellation. Live opt-in and Quantower-native checks are separate from offline suite results.

Benchmark figures are one observation on this Linux machine, not an exchange-latency, GUI-performance or production SLA claim. The bootstrap script was run again successfully to verify that it reuses the installed SDK.
