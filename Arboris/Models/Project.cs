using Arboris.Models.Graph.CXX;
using System.ComponentModel.DataAnnotations;

namespace Arboris.Models;

public record GetProject(Guid Id, string SolutionName, DateTime CreateTime, bool IsLocked);
public record ProjectReport(Guid Id, string VcProjectName, string CodeName, string? Spelling, string? CxType, string? AccessSpecifiers, string? ClassName, string? NameSpace, string? Description, string[] RelativeFilePaths)
    : BaseNodeInfo(VcProjectName, CodeName, Spelling, CxType, AccessSpecifiers, ClassName, NameSpace, RelativeFilePaths);
public record ProjectConfig([Required] string SolutionName, ProjectInfo[] ProjectInfos);
public record ProjectInfo([Required] string ProjectName, [Required] string ProjectRelativePath, string[] AdditionalIncludeDirectories, string[] PreprocessorDefinitions, string[] ClCompiles, string[] ClIncludes);
public record CreateProjectResult(string SolutionName, GetAllNodeDto[] CxxOverallGraph);
