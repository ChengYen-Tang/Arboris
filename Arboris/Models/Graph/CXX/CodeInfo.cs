namespace Arboris.Models.Graph.CXX;

public class CodeInfo
{
    public string VcProjectName { get; set; } = null!;
    public string? NameSpace { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public string? ClassName { get; set; }
    public string? Description { get; set; }
}

public record BaseNodeInfo(string VcProjectName, string CodeName, string? Spelling, string? CxType, string? ClassName, string? NameSpace, string[] RelativeFilePaths);
public record NodeOtherInfoWithLocation(string VcProjectName, string CodeName, string? Spelling, string? CxType, string? ClassName, string? NameSpace, IReadOnlySet<string>? IncludeStrings, string[] RelativeFilePaths);
