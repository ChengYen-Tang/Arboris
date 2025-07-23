namespace Arboris.Models.Analyze.CXX;
public class Location
{
    public string FilePath { get; init; }
    public uint StartLine { get; init; }
    public uint StartColumn { get; set; }
    public uint EndLine { get; init; }
    public uint EndColumn { get; set; }
    public Lazy<string?>? SourceCode { get; set; }
    public Lazy<string?>? DisplayName { get; set; }

    public Location(string filePath, uint startLine, uint startColumn, uint endLine, uint endColumn)
        => (FilePath, StartLine, StartColumn, EndLine, EndColumn) = (filePath, startLine, startColumn, endLine, endColumn);

    public static bool operator ==(Location left, Location right)
        => left.Equals(right);

    public static bool operator !=(Location left, Location right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is Location location &&
        FilePath == location.FilePath &&
        StartLine == location.StartLine &&
        StartColumn == location.StartColumn &&
        EndLine == location.EndLine &&
        EndColumn == location.EndColumn;

    public override int GetHashCode()
        => HashCode.Combine(FilePath, StartLine, StartColumn, EndLine, EndColumn);

    public string ComputeSHA256Hash()
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FilePath + StartLine + StartColumn + EndLine + EndColumn));
        return Convert.ToBase64String(hash);
    }
}
