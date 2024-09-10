// See https://aka.ms/new-console-template for more information
using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.Repositories;
using Arboris.Repositories;
using GenerateASTTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Text;

ServiceCollection services = new();

services.AddPooledDbContextFactory<ArborisDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Arboris;Trusted_Connection=True;MultipleActiveResultSets=true"));
services.AddSingleton<ICxxRepository, CxxRepository>();
services.AddSingleton<IProjectRepository, ProjectRepository>();
services.AddSingleton<CxxAggregate>();
services.AddLogging(config =>
{
    config.ClearProviders(); // 清除默認的日誌提供程序
    config.AddConsole(); // 添加控制台日誌提供程序
    config.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    config.AddFilter("Arboris", LogLevel.Debug);
});
ServiceProvider serviceProvider = services.BuildServiceProvider();

IProjectRepository projectRepository = serviceProvider.GetRequiredService<IProjectRepository>();
CxxAggregate cxxAggregate = serviceProvider.GetRequiredService<CxxAggregate>();
ILogger<Clang> logger = serviceProvider.GetRequiredService<ILogger<Clang>>();

//Guid projectId = Guid.NewGuid();
//await projectRepository.CreateProjectAsync(projectId);
//Console.WriteLine($"ProjectId: {projectId}");
//Clang clang = new(Guid.Parse("92493b91-0c6f-4510-8237-d4c9e9a9c0b0"), @"D:\Code\MHM_Library_Test\MHM_Dll", cxxAggregate, logger);
//await clang.Scan();

IDbContextFactory<ArborisDbContext> dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ArborisDbContext>>();
using ArborisDbContext dbContext = dbContextFactory.CreateDbContext();

var nodes = await dbContext.Cxx_Nodes.Select(item => new { item.Id, item.NameSpace, item.Spelling, item.CxType }).ToArrayAsync();
CodeInfo[] infos = nodes.AsParallel().Select(node =>
{
    using ArborisDbContext db = dbContextFactory.CreateDbContext();
    string? className = db.Cxx_NodeMembers.Include(item => item.Node).Where(item => item.MemberId == node.Id).Select(item => item.Node.Spelling).FirstOrDefault();
    StringBuilder sb = new();

    if (!string.IsNullOrEmpty(node.NameSpace))
        sb.Append(node.NameSpace).Append("::");
    if (!string.IsNullOrEmpty(className))
        sb.Append(className).Append("::");
    sb.Append(node.Spelling);

    return new CodeInfo
    {
        CodeName = sb.ToString(),
        CxType = node.CxType,
        NameSpace = node.NameSpace,
        Spelling = node.Spelling
    };
}).ToArray();


string fileName = "data.bson";

if (File.Exists(fileName))
    File.Delete(fileName);
using FileStream fs = File.Create(fileName);
using BsonDataWriter writer = new(fs);
JsonSerializer serializer = new();
serializer.Serialize(writer, infos);
writer.Close();
fs.Close();

//LiteContext liteContext = new();

//// 檢查infos 資料中有沒有CodeName和CxType重復，然後去重複
//infos = infos.GroupBy(item => new { item.CodeName, item.CxType }).Select(item => item.First()).ToArray();

//await liteContext.AddRangeAsync(infos);
//await liteContext.SaveChangesAsync();
