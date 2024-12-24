namespace Arboris.Models.Graph.CXX;

public record ForUtServiceFuncInfo(string FilePath, string? Spelling, string? CxType, string? Namespace, string? ClassName, string? CursorKindSpelling);
public record ForCompileDto(string FilePath, IReadOnlySet<string>? IncludeStrings);
public record ForGenerateCodeDto(string Spelling, string FilePath, string[] RelativeFilePaths);
