# Trial checklist

- [ ] User confirms actual license and expiry in Quantower UI.
- [ ] Exact selected symbol is Spot, Size semantics verified, and Connection identity recorded.
- [ ] First real native public payload captured; UTC, price, quantity, aggressor and ID inspected.
- [ ] At least 30 minutes captured; final capture-status exists with positive count.
- [ ] Duplicate/unknown-ID, drops, rejects and latency inspected, not silently ignored.
- [ ] Single source selection verified. Optional second Spot source verified only if native licensing/data availability permits it.
- [ ] Raw capture preserved for Replay; same config produces byte-identical repeated reports.
- [ ] Profile levels compared against JSON and across chart timeframes, without final-session lookahead.
- [ ] Gaps/reconnect behaviour documented separately; no claim of recovered history.
- [ ] Post-trial native restrictions and external fallback documented from real evidence.
