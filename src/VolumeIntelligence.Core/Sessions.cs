namespace VolumeIntelligence.Core;

public sealed record SessionSettings(SessionKind Kind = SessionKind.Daily, string TimeZoneId = "UTC",
    string Start = "00:00", string End = "00:00");
public sealed record SessionWindow(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public sealed class SessionCalendar
{
    private readonly SessionSettings settings;
    private readonly TimeZoneInfo zone;
    private readonly TimeOnly start, end;
    public SessionCalendar(SessionSettings settings)
    {
        if (!Enum.IsDefined(settings.Kind)) throw new ArgumentException("Invalid session kind.");
        this.settings = settings;
        zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        start = TimeOnly.ParseExact(settings.Start, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        end = TimeOnly.ParseExact(settings.End, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    }

    public SessionWindow At(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, zone).DateTime;
        DateTime date = local.Date;
        if (settings.Kind == SessionKind.Weekly)
            date = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        if (settings.Kind == SessionKind.Monthly) date = new DateTime(date.Year, date.Month, 1);
        var begin = Resolve(date + start.ToTimeSpan());
        // During a repeated DST hour, UTC membership takes precedence over wall-clock ambiguity.
        if (utc < begin)
        {
            date = settings.Kind switch { SessionKind.Weekly => date.AddDays(-7), SessionKind.Monthly => date.AddMonths(-1), _ => date.AddDays(-1) };
            begin = Resolve(date + start.ToTimeSpan());
        }
        DateTime finish = settings.Kind switch
        {
            SessionKind.Weekly => date.AddDays(7) + start.ToTimeSpan(),
            SessionKind.Monthly => date.AddMonths(1) + start.ToTimeSpan(),
            SessionKind.CustomDaily => date.AddDays(end <= start ? 1 : 0) + end.ToTimeSpan(),
            _ => date.AddDays(1) + start.ToTimeSpan()
        };
        var result = new SessionWindow(begin, Resolve(finish));
        if (utc < result.StartUtc || utc >= result.EndUtc)
            throw new OutsideSessionException("Trade is outside the configured custom session.");
        return result;
    }

    private DateTimeOffset Resolve(DateTime local)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        // Invalid spring-forward boundaries move to the first valid minute.
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
public sealed class OutsideSessionException(string message) : Exception(message);
