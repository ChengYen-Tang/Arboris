using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Arboris.DbMigrator;

public class DbMigratorHostedService(
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<DbMigratorHostedService> logger,
    ArborisDbContext dbContext) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
