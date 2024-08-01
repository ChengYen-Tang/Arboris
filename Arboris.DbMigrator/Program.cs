using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Arboris.DbMigrator;

public class Program
{
    static async Task Main(string[] args)
    {
        //        Log.Logger = new LoggerConfiguration()
        //            .MinimumLevel.Information()
        //            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        //#if DEBUG
        //                .MinimumLevel.Override("Arboris", LogEventLevel.Debug)
        //#else
        //                .MinimumLevel.Override("Arboris", LogEventLevel.Information)
        //#endif
        //                .Enrich.FromLogContext()
        //            .WriteTo.Async(c => c.File("Logs/logs.txt"))
        //            .WriteTo.Async(c => c.Console())
        //            .CreateLogger();

        await CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            //.ConfigureLogging((context, logging) => logging.ClearProviders())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DbMigratorHostedService>();
                services.AddDbContext<ArborisDbContext>(options =>
                options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Singleton);
            });
}
