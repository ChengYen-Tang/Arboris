namespace Arboris.Models.Analyze.CXX;
public class Location
{
    public string FilePath { get; init; }
    public uint StartLine { get; init; }
    public uint EndLine { get; init; }
    public string? SourceCode { get; set; }
    public string? CodeDefine { get; set; }

    public Location(string filePath, uint startLine, uint endLine)
        => (FilePath, StartLine, EndLine) = (filePath, startLine, endLine);

    public static bool operator ==(Location left, Location right)
        => left.Equals(right);

    public static bool operator !=(Location left, Location right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is Location location &&
        FilePath == location.FilePath &&
        StartLine == location.StartLine &&
        EndLine == location.EndLine;

    public override int GetHashCode()
        => HashCode.Combine(FilePath, StartLine, EndLine);
}
