namespace Arboris.Models;

public record CreateProject(string Name);
public record GetProject(Guid Id, string Name, DateTime CreateTime);
