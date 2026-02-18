using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.Queries;
using YouthDataManager.FollowUp.Service.Implementations.Commands;
using YouthDataManager.FollowUp.Service.Implementations.Queries;

namespace YouthDataManager.FollowUp.Service.DependencyInjection;

public static class DependencyInjectionExtensions
{
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
