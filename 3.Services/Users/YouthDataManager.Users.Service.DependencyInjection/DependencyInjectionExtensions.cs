using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Implementations;

namespace YouthDataManager.Users.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddUsersServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
