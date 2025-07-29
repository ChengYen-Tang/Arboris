namespace Arboris.Models.Graph.CXX;

public record NodeInfoWithDependency(NodeSourceCode[] SourceCode, string? NameSpace, string? Spelling, string? AccessSpecifiers, Guid? ClassNodeId, Guid[] Dependencies, string? CursorKindSpelling, bool NeedGenerate, string VcProjectName);
public record NodeSourceCode(string FilePath, string? DisplayName, string? Code, bool IsDefine);
