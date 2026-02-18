using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Shared.Service.Implementations;

namespace YouthDataManager.Shared.Service.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            return services;
        }
    }
}
