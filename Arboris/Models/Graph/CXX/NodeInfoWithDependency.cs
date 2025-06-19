namespace Arboris.Models.Graph.CXX;

public record NodeInfoWithDependency(NodeSourceCode[] SourceCode, string? NameSpace, string? Spelling, string? AccessSpecifiers, IReadOnlySet<string>? IncludeStrings, Guid? ClassNodeId, Guid[] Dependencies);
public record NodeSourceCode(string FilePath, string? DisplayName, string? Code, bool IsDefine);
