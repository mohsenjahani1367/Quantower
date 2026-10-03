# Troubleshooting

| Symptom | Action |
|---|---|
| SDK not found or global.json mismatch | Run the official checksum-verified setup script; return SDK version and build log, not environment values. |
| Quantower DLL missing | Use the real installed DLL path. Do not substitute a guessed stub or a fake API assembly. |
| Windows adapter compiler error | Return the actual compiler log and API hash. Do not install a failed or stale build. |
| HTTP 403 / denied public endpoint | Respect access policy. Cloud domain additions were saved as a draft, not activated by the agent. No regional workaround. |
| No live trades | Check report.json and health.jsonl; zero-data exit is nonzero, not a successful connection. |
| Native no-data/rejected events | Confirm source and subscription; inspect actual Size/Time.Kind/aggressor/ID semantics. Do not relabel unknown values. |
| Native capture incomplete | Wait for final metadata after Stop; do not kill Quantower while draining. |
| Replay source excluded | Exact venue, connection, market, symbol, base and quote must match mapping/settings. |
| Conflicting ID | Treat as data integrity failure; do not disable deduplication. |
| LateExcluded > 0 | Report degraded data; preserve arrival order. Increasing reorder window is an explicit policy change requiring revalidation. |
| Capacity / price-span limit | Rotate bounded runs or choose an appropriate row size; do not silently evict IDs. |
| Indicator has no lines | A source session must be fully closed and the chart mapped exactly; check UTC and file paths. No developing profile is implemented. |
