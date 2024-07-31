namespace Arboris.Models;

public record CreateProject(string Name, DateTime CreateTime);
public record GetProject(Guid Id, string Name, DateTime CreateTime);
