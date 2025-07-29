using Arboris.Models.Graph.CXX;
using System.ComponentModel.DataAnnotations;

namespace Arboris.Models;

public record GetProject(Guid Id, string SolutionName, DateTime CreateTime, bool IsLocked);
public record ProjectReport(Guid Id, string VcProjectName, string CodeName, string? Spelling, string? CxType, string? AccessSpecifiers, string? ClassName, string? NameSpace, string? Description, string[] RelativeFilePaths)
    : BaseNodeInfo(VcProjectName, CodeName, Spelling, CxType, AccessSpecifiers, ClassName, NameSpace, RelativeFilePaths);
public record ProjectConfig([Required] string SolutionName, ProjectInfo[] ProjectInfos)
{
    public void SetProjectDependencies()
    {
        Dictionary<string, string> ouputLibNameMapProjectName = [];
        foreach (ProjectInfo projectInfo in ProjectInfos)
        {
            if (!string.IsNullOrEmpty(projectInfo.OuputLibName))
                ouputLibNameMapProjectName[projectInfo.OuputLibName.ToLower()] = projectInfo.ProjectName;
        }
        foreach (ProjectInfo projectInfo in ProjectInfos)
        {
            projectInfo.SetProjectDependencies(ouputLibNameMapProjectName);
        }
    }
}

public record ProjectInfo([Required] string ProjectName, [Required] string ProjectRelativePath, string? OuputLibName, string[] AdditionalIncludeDirectories, string[] PreprocessorDefinitions, string[] AdditionalDependencies, string[] ClCompiles, string[] ClIncludes)
{
    public IReadOnlyList<string> ProjectDependencies { get; private set; } = [];

    public void SetProjectDependencies(IDictionary<string, string> ouputLibNameMapProjectName)
    {
        List<string> dependencies = [];
        dependencies.Add(ProjectName);
        foreach (string dependency in AdditionalDependencies)
            if (ouputLibNameMapProjectName.TryGetValue(dependency.ToLower(), out string? projectName) && !dependencies.Contains(projectName))
                dependencies.Add(projectName!);

        ProjectDependencies = dependencies.AsReadOnly();
    }
}

public record CreateProjectResult(string SolutionName, GetAllNodeDto[] CxxOverallGraph);
