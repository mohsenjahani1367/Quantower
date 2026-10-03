# Decisions — 2026-10-03

- Preserve the original specification verbatim; its roadmap is not an assertion of delivered features.
- Use the existing isolated checkout; no worktree is needed for routine cloud tasks.
- Independent code targets net10.0; adapters target net10.0-windows based on supplied actual assembly evidence.
- Pin SDK 10.0.401 and verify official artifact SHA-512. Keep TLS verification enabled.
- No third-party package dependencies. An executable assertion suite reports actual test counts/nonzero failures; zero-test `dotnet test` is not validation.
- Start with public Binance Spot, matching the currently reported native market. Other providers are roadmap work.
- Quote notional is in the exact quote asset. No silent USD/USDT/USDC equivalence. Explicit causal FX conversion exists in Core.
- POC tie: lower bin. VA expansion: one adjacent bin, greater neighbour volume first, down on ties; absent bins are zero.
- POC and VAL are lower edges; VAH is the upper edge of the highest VA bin. No private-MMT algorithm parity claim.
- Unknown aggressor/count stays unknown. Exact dedup capacity fails rather than silently evicting IDs.
- Native/Indicator assemblies are separate for Strategies/Indicators discovery folders.
- Existing loopback Bridge and credentials are untouched. Its uploaded source does not connect the cloud to the desktop.
