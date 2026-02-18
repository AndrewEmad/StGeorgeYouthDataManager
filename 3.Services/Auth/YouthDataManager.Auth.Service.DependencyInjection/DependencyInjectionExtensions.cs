using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Implementations.Commands;
using YouthDataManager.Auth.Service.Implementations.Services;

namespace YouthDataManager.Auth.Service.DependencyInjection;

public static class DependencyInjectionExtensions
{
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
}
