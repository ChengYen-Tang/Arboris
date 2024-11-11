using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.Repositories;
using Arboris.Repositories;
using Arboris.Service.Modules;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:o} {RequestId,13} [{Level:u3}] [{SourceContext} {Method}] {Message} ({EventId:x8}){NewLine}{Exception}")
    .CreateLogger();
builder.Services.AddSerilog();

// Add services to the container.
builder.Services.AddPooledDbContextFactory<ArborisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ICxxRepository, CxxRepository>();
builder.Services.AddScoped<ProjectAggregate>();
builder.Services.AddScoped<CxxAggregate>();
builder.Services.AddScoped<ClangFactory>();
builder.Services.AddScoped<Arboris.Domain.Project>();
builder.Services.AddSingleton<GarbageCollection>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Hangfire
builder.Services.AddHangfire(configuration => configuration
.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseColouredConsoleLogProvider()
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage(new InMemoryStorageOptions
    {
        MaxExpirationTime = TimeSpan.FromDays(30)
    }));
builder.Services.AddHangfireServer();
#endregion

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = long.MaxValue;
    await next.Invoke();
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();
app.MapHangfireDashboard("/Jobs");
GarbageCollection? gc = app.Services.GetService<GarbageCollection>();
gc?.AddToCrontab();

await app.RunAsync();
