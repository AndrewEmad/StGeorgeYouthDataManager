using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Implementations.Commands;
using YouthDataManager.Auth.Service.Implementations.Services;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.Queries;
using YouthDataManager.FollowUp.Service.Implementations.Commands;
using YouthDataManager.FollowUp.Service.Implementations.Queries;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Reports.Service.Implementations.Queries;
using YouthDataManager.Reports.Service.Implementations.Services;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Shared.Service.Implementations;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.Students.Service.Implementations.Commands;
using YouthDataManager.Students.Service.Implementations.Queries;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Implementations;

namespace YouthDataManager.Services.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        return services;
    }

    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtConfiguration>(configuration.GetSection("Jwt"));
        return services
            .AddAuthCommandsService(configuration);
    }

    public static IServiceCollection AddAuthCommandsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IAuthCommandsService, AuthCommandsService>();
        return services;
    }

    public static IServiceCollection AddUsersServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IServiceCollection AddStudentsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddStudentsCommandsService(configuration)
            .AddStudentsQueriesService(configuration);
    }

    public static IServiceCollection AddStudentsCommandsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddTransient<IStudentCommandsService, StudentCommandsService>()
            .AddTransient<IStudentRemovalRequestService, StudentRemovalRequestService>()
            .AddTransient<IStudentAssignmentRequestService, StudentAssignmentRequestService>()
            .AddTransient<IStudentAdditionRequestService, StudentAdditionRequestService>()
            .AddTransient<IAttendanceService, AttendanceService>();
    }

    public static IServiceCollection AddStudentsQueriesService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddTransient<IStudentQueriesService, StudentQueriesService>();
    }

    public static IServiceCollection AddReportsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddReportsQueriesService(configuration);
    }

    public static IServiceCollection AddReportsQueriesService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ExcelExportService>();
        services.AddTransient<IReportQueriesService, ReportQueriesService>();
        return services;
    }

    public static IServiceCollection AddFollowUpServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddFollowUpCommandsService(configuration)
            .AddFollowUpQueriesService(configuration);
    }

    public static IServiceCollection AddFollowUpCommandsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddTransient<IFollowUpCommandsService, FollowUpCommandsService>();
    }

    public static IServiceCollection AddFollowUpQueriesService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddTransient<IFollowUpQueriesService, FollowUpQueriesService>();
    }
}
