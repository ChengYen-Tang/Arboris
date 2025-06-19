using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[Index(nameof(StartLine), nameof(EndLine), nameof(StartColumn), nameof(EndColumn), nameof(NodeId), nameof(Id))]
[Index(nameof(NodeId), nameof(StartLine), nameof(EndLine), nameof(StartColumn))]
[Index(nameof(NodeId), nameof(Id), nameof(StartLine), nameof(EndLine))]
[Index(nameof(EndColumn), nameof(NodeId), nameof(Id), nameof(StartLine), nameof(EndLine), nameof(StartColumn))]
[Index(nameof(EndColumn), nameof(StartLine), nameof(EndLine))]
[Index(nameof(NodeId), nameof(EndLine), nameof(StartColumn), nameof(EndColumn))]
[Index(nameof(EndLine), nameof(StartColumn), nameof(EndColumn))]
[Index(nameof(StartColumn), nameof(NodeId), nameof(Id), nameof(StartLine), nameof(EndLine))]
[Index(nameof(StartColumn), nameof(StartLine), nameof(EndLine), nameof(EndColumn), nameof(NodeId))]
[Index(nameof(Id), nameof(StartLine), nameof(EndLine), nameof(StartColumn), nameof(EndColumn))]
[Index(nameof(EndColumn), nameof(StartLine), nameof(StartColumn))]
[Index(nameof(NodeId), nameof(StartLine), nameof(EndLine), nameof(StartColumn))]
public class DefineLocation : Location
{
}

[Index(nameof(EndColumn), nameof(StartLine), nameof(EndLine), nameof(StartColumn), nameof(NodeId))]
[Index(nameof(Id), nameof(StartLine), nameof(EndLine), nameof(StartColumn))]
[Index(nameof(StartLine), nameof(EndLine), nameof(StartColumn), nameof(EndColumn), nameof(NodeId))]
[Index(nameof(StartColumn), nameof(StartLine))]
[Index(nameof(StartColumn), nameof(NodeId), nameof(Id), nameof(StartLine))]
[Index(nameof(EndLine), nameof(StartColumn), nameof(EndColumn), nameof(StartLine), nameof(Id), nameof(NodeId))]
[Index(nameof(EndColumn), nameof(NodeId), nameof(Id), nameof(StartLine), nameof(EndLine))]
[Index(nameof(NodeId), nameof(Id), nameof(StartLine), nameof(StartColumn))]
[Index(nameof(NodeId), nameof(EndLine), nameof(StartColumn), nameof(EndColumn))]
public class ImplementationLocation : Location
{
}

public class Location
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string FilePath { get; set; } = null!;
    [Required]
    public uint StartLine { get; set; }
    [Required]
    public uint StartColumn { get; set; }
    [Required]
    public uint EndLine { get; set; }
    [Required]
    public uint EndColumn { get; set; }

    public string? DisplayName { get; set; }
    public string? SourceCode { get; set; }

    [Required]
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public Location()
        => Id = Guid.NewGuid();

    public string ComputeSHA256Hash()
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FilePath + StartLine + StartColumn + EndLine + EndColumn));
        return Convert.ToBase64String(hash);
    }
}
