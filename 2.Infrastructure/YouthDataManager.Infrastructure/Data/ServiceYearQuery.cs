using System.Linq.Expressions;
using YouthDataManager.Domain.Entities;
namespace YouthDataManager.Infrastructure.Data;

public static class ServiceYearQuery
{
    public static IQueryable<T> ForServiceYear<T>(this IQueryable<T> query, AppDbContext context) where T:class
    {
        if(!context.HasServiceYear)return query;
        var field=typeof(T).Name switch
        {
            nameof(CallLog) => "CallDate",
            nameof(HomeVisit) => "VisitDate",
            nameof(StudentAttendance) => "Date",
            nameof(ServantAttendance) => "Date",
            nameof(StudentAssignmentRequest) => "RequestedAt",
            nameof(StudentRemovalRequest) => "RequestedAt",
            nameof(StudentAdditionRequest) => "RequestedAt",
            nameof(StudentEditLog) => "UpdatedAt",
            nameof(ActivityLog) => "Timestamp",
            nameof(AuditEntry) => "Timestamp",
            nameof(FollowUpTask) => "DueAt",
            nameof(MeetingSession) => "Date",
            _ => null
        };
        if(field==null)return query; // Current people/assignments remain available in every year.
        var dateOnly=typeof(T)==typeof(MeetingSession) || typeof(T)==typeof(StudentAttendance) || typeof(T)==typeof(ServantAttendance);
        var start=dateOnly ? context.ServiceYearFirstDate : context.ServiceYearStart;
        var end=dateOnly ? context.ServiceYearNextDate : context.ServiceYearEnd;
        var row=Expression.Parameter(typeof(T),"row");var value=Expression.Property(row,field);
        var condition=Expression.AndAlso(Expression.GreaterThanOrEqual(value,Expression.Constant(start)),Expression.LessThan(value,Expression.Constant(end)));
        return query.Where(Expression.Lambda<Func<T,bool>>(condition,row));
    }
}
