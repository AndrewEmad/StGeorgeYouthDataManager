using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Repositories.NoTracking;
using YouthDataManager.Infrastructure.Repositories.Tracking;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Reports.Service.Abstractions.Data;

namespace YouthDataManager.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MainDatabase");

        services.AddDbContext<AppDbContext>(options => 
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services
            // Unit of Work
            .AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>())
            
            // Seeders
            .AddScoped<DatabaseSeeder>()
            
            // Tracking repositories
            .AddTransient<IStudentRepository, StudentRepository>()
            .AddTransient<ICallLogRepository, CallLogRepository>()
            .AddTransient<IHomeVisitRepository, HomeVisitRepository>()
            .AddTransient<IStudentRemovalRequestRepository, StudentRemovalRequestRepository>()
            .AddTransient<IStudentAssignmentRequestRepository, StudentAssignmentRequestRepository>()
            .AddTransient<IStudentAdditionRequestRepository, StudentAdditionRequestRepository>()
            .AddTransient<IStudentAttendanceRepository, StudentAttendanceRepository>()
            .AddTransient<IServantAttendanceRepository, ServantAttendanceRepository>()
            .AddTransient<IStudentEditLogRepository, StudentEditLogRepository>()
            
            // NoTracking repositories
            .AddTransient<IStudentNoTrackingRepository, StudentNoTrackingRepository>()
            .AddTransient<ICallLogNoTrackingRepository, CallLogNoTrackingRepository>()
            .AddTransient<IHomeVisitNoTrackingRepository, HomeVisitNoTrackingRepository>()
            .AddTransient<IActivityLogNoTrackingRepository, ActivityLogNoTrackingRepository>()
            .AddTransient<IStudentEditLogNoTrackingRepository, StudentEditLogNoTrackingRepository>()
            .AddTransient<IReportDataProvider, ReportDataProvider>();
    }
}
