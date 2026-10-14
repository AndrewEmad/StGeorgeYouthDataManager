using YouthDataManager.Domain.Exceptions;
namespace YouthDataManager.Shared.Service.Abstractions;

public sealed record ServiceYear(DateTime Start, DateTime LastDay)
{
    public DateTime AsOf(DateTime utcNow)
    {
        var today=TimeZoneInfo.ConvertTimeFromUtc(utcNow,BusinessTime.Zone).Date;
        return LastDay<today ? LastDay : today;
    }
    public DateTime UtcStart => BusinessTime.UtcStart(Start);
    public DateTime UtcEnd => BusinessTime.UtcStart(LastDay.AddDays(1));
    public static ServiceYear From(int year)
    {
        if(year is <1900 or >9997) throw new DomainValidationException("عام الخدمة غير صحيح.");
        var boundary=new DateTime(year,9,12);
        var next=boundary.AddYears(1);
        var after=((int)DayOfWeek.Friday-(int)boundary.DayOfWeek+7)%7;
        var before=((int)next.DayOfWeek-(int)DayOfWeek.Friday+7)%7;
        return new(boundary.AddDays(after==0 ? 7 : after),next.AddDays(-(before==0 ? 7 : before)));
    }
}
