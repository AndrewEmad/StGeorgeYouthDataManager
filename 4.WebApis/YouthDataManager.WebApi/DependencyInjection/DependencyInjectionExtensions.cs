using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Infrastructure.DependencyInjection;
using YouthDataManager.Services.DependencyInjection;

namespace YouthDataManager.WebApi.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddInfrastructure(configuration)
            .AddAuthServices(configuration)
            .AddStudentsServices(configuration)
            .AddFollowUpServices(configuration)
            .AddReportsServices(configuration)
            .AddUsersServices()
            .AddSharedServices();
    }
}
