using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApplifyLab.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations` build the DbContext directly instead of spinning up the full
/// app host (which would otherwise eagerly connect to Redis/Hangfire just to design a migration).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=applifylab;Username=applifylab;Password=applifylab";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
