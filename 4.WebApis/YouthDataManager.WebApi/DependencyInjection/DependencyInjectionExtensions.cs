using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YouthDataManager.Infrastructure.DependencyInjection;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Services.DependencyInjection;
using YouthDataManager.WebApi.Configuration;

namespace YouthDataManager.WebApi.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IPostConfigureOptions<PhotoUploadOptions>, PostConfigurePhotoUploadOptions>();
        return services
            .AddInfrastructure(configuration)
            .AddAuthServices(configuration)
            .AddStudentsServices(configuration)
            .AddFollowUpServices(configuration)
            .AddReportsServices(configuration)
            .AddUsersServices()
            .AddPhotoServices(configuration)
            .AddSharedServices();
    }
}
