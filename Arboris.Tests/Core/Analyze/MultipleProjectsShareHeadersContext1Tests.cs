using Arboris.Analyze.CXX;
using Arboris.Models;
using Arboris.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Arboris.Tests.Core.Analyze;

[TestClass]
public class MultipleProjectsShareHeadersContext1Tests
{
    private IServiceProvider serviceProvider;
    private IDbContextFactory<ArborisDbContext> contextFactory { get => serviceProvider.GetRequiredService<IDbContextFactory<ArborisDbContext>>(); }

    public MultipleProjectsShareHeadersContext1Tests()
    {
        string testDataFolder = Path.Combine(AppContext.BaseDirectory, "Core", "Analyze", "TestData");
        string solutionPath = Path.Combine(testDataFolder, "Context1");
        string solutionConfigPath = Path.Combine(solutionPath, "Config.json");
        string solutionConfigJson = File.ReadAllText(solutionConfigPath);
        ProjectConfig solutionConfig = JsonSerializer.Deserialize<ProjectConfig>(solutionConfigJson);

        serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(sp => DBContextInit.GetArborisDbContextFactoryAsync().Result)
            .AddScoped<IProjectRepository, ProjectRepository>()
            .AddScoped<ICxxRepository, CxxRepository>()
            .AddScoped<ProjectAggregate>()
            .AddScoped<CxxAggregate>()
            .AddScoped<ClangFactory>()
            .AddScoped<Arboris.Domain.Project>()
            .BuildServiceProvider();

        IProjectRepository projectRepository = serviceProvider.GetRequiredService<IProjectRepository>();
        ClangFactory clangFactory = serviceProvider.GetRequiredService<ClangFactory>();

        Guid projectId = Guid.NewGuid();
        projectRepository.CreateProjectAsync(projectId, projectId.ToString()).Wait();
        Console.WriteLine($"ProjectId: {projectId}");

        solutionConfig.SetProjectDependencies();
        clangFactory.Analyze(projectId, solutionConfig.ProjectInfos, solutionPath).Wait();
    }

    [TestMethod]
    public async Task TestMemberLinked()
    {
        ArborisDbContext dbContext = await contextFactory.CreateDbContextAsync();

        Guid classNode = await dbContext.Cxx_Nodes.Where(item => item.VcProjectName == "Motion" && item.Spelling == "Motion" && item.CursorKindSpelling == "ClassDecl").Select(item => item.Id).FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, classNode);

        Guid member1 = await dbContext.Cxx_Nodes.Where(item => item.VcProjectName == "Motion" && item.Spelling == "run" && item.CursorKindSpelling == "CXXMethod").Select(item => item.Id).FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, member1);
        Guid member2 = await dbContext.Cxx_Nodes.Where(item => item.VcProjectName == "Offline_Motion" && item.Spelling == "run" && item.CursorKindSpelling == "CXXMethod").Select(item => item.Id).FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, member2);

        Guid[] membersId = await dbContext.Cxx_NodeMembers
            .Where(item => item.NodeId == classNode)
            .Select(item => item.MemberId)
            .ToArrayAsync();
        Assert.AreEqual(2, membersId.Length, "Expected 2 members linked to the class node");

        Assert.IsTrue(membersId.Contains(member1), "Member 'run' from 'Motion' project not linked to class node");
        Assert.IsTrue(membersId.Contains(member2), "Member 'run' from 'Offline_Motion' project not linked to class node");
    }

    [TestMethod]
    public async Task TestDependencyLinked()
    {
        ArborisDbContext dbContext = await contextFactory.CreateDbContextAsync();

        Guid member1 = await dbContext.Cxx_Nodes.Where(item => item.VcProjectName == "Motion" && item.Spelling == "run" && item.CursorKindSpelling == "CXXMethod").Select(item => item.Id).FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, member1);
        Guid member2 = await dbContext.Cxx_Nodes.Where(item => item.VcProjectName == "Offline_Motion" && item.Spelling == "run" && item.CursorKindSpelling == "CXXMethod").Select(item => item.Id).FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, member2);

        Guid dllMain1 = await dbContext.Cxx_Nodes
            .Where(item => item.VcProjectName == "Context1" && item.Spelling == "DllMain" && item.CursorKindSpelling == "FunctionDecl")
            .Select(item => item.Id)
            .FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, dllMain1, "Expected 'DllMain' from 'Context1' project to be found");
        Guid dllMain2 = await dbContext.Cxx_Nodes
            .Where(item => item.VcProjectName == "Offline_Motion" && item.Spelling == "DllMain" && item.CursorKindSpelling == "FunctionDecl")
            .Select(item => item.Id)
            .FirstOrDefaultAsync();
        Assert.AreNotEqual(Guid.Empty, dllMain2, "Expected 'DllMain' from 'Offline_Motion' project to be found");

        Guid[] dependencies1 = await dbContext.Cxx_NodeDependencies
            .Where(item => item.FromId == member1)
            .Select(item => item.NodeId)
            .ToArrayAsync();
        Assert.AreEqual(0, dependencies1.Length, "Expected no dependencies for member 'run' in 'Motion' project");
        Guid[] dependencies2 = await dbContext.Cxx_NodeDependencies
            .Where(item => item.FromId == member2)
            .Select(item => item.NodeId)
            .ToArrayAsync();
        Assert.AreEqual(2, dependencies2.Length, "Expected 1 dependency for member 'run' in 'Offline_Motion' project");
        Assert.IsTrue(dependencies2.Contains(dllMain1), "Dependency 'DllMain' from 'Context1' project not linked to member 'run' in 'Offline_Motion' project");
        Assert.IsTrue(dependencies2.Contains(dllMain2), "Dependency 'DllMain' from 'Offline_Motion' project not linked to member 'run' in 'Offline_Motion' project");
    }
}
