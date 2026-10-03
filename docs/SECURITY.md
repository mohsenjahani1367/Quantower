# Security

Public market data only. No account, balance, orders, trading, withdrawal or private-key API. Binance recording accepts no API-key parameter.

Bridge token stays local; never send it to cloud/chat or include authentication files in handoffs. Existing Bridge is unchanged/loopback-only; no tunnel/public exposure is required.

Share public data fixtures and build/test logs, not environments, credential stores or account files. The old Bridge source constant secret is not copied or deployed.

Official SDK downloads keep TLS and verify SHA-512. Local installation backs up project DLLs and never replaces BusinessLayer.dll. Verify native Spot/base-size mapping before normalized capture.
