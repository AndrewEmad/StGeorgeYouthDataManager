using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YouthDataManager.Infrastructure.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("YDM_MIGRATION_CONNECTION") ??
            "Server=(localdb)\\MSSQLLocalDB;Database=YDM_Design;Integrated Security=true;MultipleActiveResultSets=false";
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
    }
}
