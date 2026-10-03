using VolumeIntelligence.Core;

namespace VolumeIntelligence.Providers;

public enum SourceState { Connecting, Connected, Reconnecting, Gapped, Stopped, Failed }
public sealed record SourceHealth(SourceKey Source, SourceState State, DateTimeOffset ObservedAtUtc,
    long Messages, int Reconnects, string Detail);
public interface ISourceHealthMonitor { SourceHealth Health { get; } }
public sealed record ExchangeCapabilities(string Venue, MarketType MarketType, string TradeKind,
    bool AggressorSide, bool HistoricalTradesImplemented, bool L2Implemented, bool RequiresAccount,
    string Evidence, string Limitations);
public interface IExchangeCapabilityProvider { ExchangeCapabilities Capabilities { get; } }
