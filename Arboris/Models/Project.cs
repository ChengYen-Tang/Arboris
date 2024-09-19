namespace Arboris.Models;

public record GetProject(Guid Id, DateTime CreateTime);
public record ProjectReport(Guid Id, string CodeName, string? Spelling, string? CxType, string? ClassName, string? NameSpace, string? Description, string[] RelativeFilePaths);
