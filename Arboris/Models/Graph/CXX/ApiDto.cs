namespace Arboris.Models.Graph.CXX;

public record GetAllNodeDto(Guid Id, string? CursorKindSpelling, bool NeedGenerate);

public record NodeLines(Guid Id, uint StartLine, uint EndLine, string? Code);
