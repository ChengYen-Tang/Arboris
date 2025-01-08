using Arboris.Models.Graph.CXX;

namespace Arboris.Models;

public record GetProject(Guid Id, string SolutionName, DateTime CreateTime, bool IsLocked);
public record ProjectReport(Guid Id, string VcProjectName, string CodeName, string? Spelling, string? CxType, string? ClassName, string? NameSpace, string? Description, string[] RelativeFilePaths)
    : BaseNodeInfo(VcProjectName, CodeName, Spelling, CxType, ClassName, NameSpace, RelativeFilePaths);
public record ProjectConfig(string SolutionName, List<ProjectInfo> ProjectInfos);
public record ProjectInfo(string ProjectName, string SourcePath, string[] AdditionalIncludeDirectories, string[] SourceCodePath);
public record CreateProjectResult(string SolutionName, OverallGraph CxxOverallGraph);
