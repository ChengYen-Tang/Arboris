using Arboris.Repositories;
using Arboris.Service.Controllers;
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

        string[] projects = (await projectRepository.GetProjectsAsync()).AsParallel().Select(item => item.Id.ToString()).ToArray();
        // Get all folder from ProjectController.CacheDirectory
        IEnumerable<string> needDeleteFolder = Directory.GetDirectories(ProjectController.CacheDirectory).AsParallel().Where(item => !projects.Contains(Path.GetDirectoryName(item)));
        foreach (string folder in needDeleteFolder)
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Directory.Delete Failed. Error message: {ErrorMessage}", ex.Message);
            }
        }
    }
}
