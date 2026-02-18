using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Reports.Service.Implementations.Queries;
using YouthDataManager.Reports.Service.Implementations.Services;

namespace YouthDataManager.Reports.Service.DependencyInjection;

public static class DependencyInjectionExtensions
{
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
}
