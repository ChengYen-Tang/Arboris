namespace Arboris.Models.Code;

public record Location(string FilePath, uint StartLine, uint StartColumn, uint EndLine, uint EndColumn);
