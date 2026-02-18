using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.Students.Service.Implementations.Commands;
using YouthDataManager.Students.Service.Implementations.Queries;

namespace YouthDataManager.Students.Service.DependencyInjection;

public static class DependencyInjectionExtensions
{
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
            .AddTransient<IStudentRemovalRequestService, StudentRemovalRequestService>();
    }

    public static IServiceCollection AddStudentsQueriesService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddTransient<IStudentQueriesService, StudentQueriesService>();
    }
}
