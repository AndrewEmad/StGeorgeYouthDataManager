using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.DependencyInjection;
using YouthDataManager.FollowUp.Service.DependencyInjection;
using YouthDataManager.Infrastructure.DependencyInjection;
using YouthDataManager.Reports.Service.DependencyInjection;
using YouthDataManager.Students.Service.DependencyInjection;
using YouthDataManager.Shared.Service.DependencyInjection;
using YouthDataManager.Users.DependencyInjection;

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
