namespace YouthDataManager.Shared.Service.Abstractions;

public static class BusinessTime
{
    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
    public static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,Zone);
    public static DateTime UtcStart(DateTime date)
    {
        return UtcFromLocal(date.Date);
    }
    public static DateTime UtcFromLocal(DateTime date)
    {
        var local = DateTime.SpecifyKind(date,DateTimeKind.Unspecified);
        // Cairo's spring transition can skip midnight. A calendar day starts at
        // its first valid instant; an ambiguous boundary includes both folds.
        for (var minute = 0; Zone.IsInvalidTime(local) && minute < 180; minute++) local = local.AddMinutes(1);
        if (Zone.IsAmbiguousTime(local)) return new DateTimeOffset(local,Zone.GetAmbiguousTimeOffsets(local).Max()).UtcDateTime;
        return TimeZoneInfo.ConvertTimeToUtc(local,Zone);
    }
    public static (DateTime Start,DateTime End) Week(int startsOn = 5)
    {
        var now = LocalNow;
        var start = now.Date.AddDays(-(((int)now.DayOfWeek-startsOn+7)%7));
        return (UtcStart(start),UtcStart(start.AddDays(7)));
    }
}
