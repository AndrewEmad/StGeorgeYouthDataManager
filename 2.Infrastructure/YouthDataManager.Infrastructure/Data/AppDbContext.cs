using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;

namespace YouthDataManager.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<HomeVisit> HomeVisits => Set<HomeVisit>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<StudentRemovalRequest> StudentRemovalRequests => Set<StudentRemovalRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.HasDefaultSchema("dbo");
        
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Simple audit logic can be added here if desired, otherwise base is fine
        return await base.SaveChangesAsync(cancellationToken);
    }
}
