# Implementation plan

The repository originally contained only README.md. First delivery is an independent working slice, not completion of the full specification.

1. Inspect evidence and target framework. Complete: supplied BusinessLayer metadata identifies .NET 10; official SDK 10.0.401 installed with SHA-512 validation.
2. Implement explicit source/instrument identities, normalization, selection, deterministic ordering/deduplication, profiles and JSONL recording/replay. Complete within documented scope.
3. Implement Binance Spot public provider. Parser tested with synthetic contract fixtures; actual delivery requires permitted network and live validation.
4. Prepare native capture and Quantower replay-level sources. Actual Windows API build and smoke test outstanding.
5. Run build, numbered test suite and golden CLI replay with byte-identical repeat. Save evidence and Windows handoff.
6. After local evidence: fix API differences; verify native units/timestamps/aggressor/IDs; capture 30 minutes and compare replay.

Required cloud checks: build, calculation/normalization/session tests, replay CLI. Optional cloud check: live public WebSocket capture. Required before claiming Quantower readiness: real API build, discovery, native payload, stop/unsubscribe, chart verification.
