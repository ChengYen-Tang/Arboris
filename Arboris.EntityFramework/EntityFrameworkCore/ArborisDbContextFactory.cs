using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Arboris.EntityFramework.EntityFrameworkCore;

public class ArborisDbContextFactory : IDesignTimeDbContextFactory<ArborisDbContext>
{
    public ArborisDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<ArborisDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new ArborisDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Arboris.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
