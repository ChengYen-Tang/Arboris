using Arboris.Repositories;
using Hangfire;

namespace Arboris.Service.Modules;

public class GarbageCollection(
    ILogger<GarbageCollection> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider)
{
    public void AddToCrontab()
        => RecurringJob.AddOrUpdate("GC", () => Worker(), Cron.Daily, new() { TimeZone = TimeZoneInfo.Local });

    public async Task Worker()
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider service = scope.ServiceProvider;
        IProjectRepository projectRepository = service.GetRequiredService<IProjectRepository>();
        try
        {
            int retentionDays = configuration.GetValue("RetentionDays", 7);
            await projectRepository.DeleteTooOldProjectAsync(retentionDays);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "projectRepository.DeleteTooOldProjectAsync Failed. Error message: {ErrorMessage}", ex.Message);
        }
    }
}
