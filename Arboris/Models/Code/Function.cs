namespace Arboris.Models.Code;

public record Function(string FunctionName, Location Location, Dictionary<string, List<Location>> Types);
