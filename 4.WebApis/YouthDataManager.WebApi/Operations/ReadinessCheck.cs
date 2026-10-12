using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Photo.Service.Abstractions;

namespace YouthDataManager.WebApi.Operations;

public sealed class ReadinessCheck(AppDbContext db,IOptions<PhotoUploadOptions> photos) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken ct=default)
    {
        try
        {
            if(!await db.Database.CanConnectAsync(ct))return HealthCheckResult.Unhealthy("Database unavailable.");
            if((await db.Database.GetPendingMigrationsAsync(ct)).Any())return HealthCheckResult.Unhealthy("Schema deployment required.");
            var root=Path.GetFullPath(photos.Value.UploadBasePath);Directory.CreateDirectory(root);
            var file=Path.Combine(root,$".readiness-{Guid.NewGuid():N}.tmp");
            try{await File.WriteAllTextAsync(file,"storage probe",ct);if(await File.ReadAllTextAsync(file,ct)!="storage probe")return HealthCheckResult.Unhealthy("Photo storage unavailable.");}
            finally{if(File.Exists(file))File.Delete(file);}
            return HealthCheckResult.Healthy();
        }
        catch(Exception) when(!ct.IsCancellationRequested){return HealthCheckResult.Unhealthy("A required dependency is unavailable.");}
    }
}
