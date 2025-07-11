using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;


// ──────────────── Define ────────────────
// ❶ 針對「檔案+精確座標」做唯一索引 (大部分查詢)
// ❷ 因為一個 Node 只有一筆 DefineLocation → NodeId 唯一
// ❸ 若已先有 NodeId 再比對座標，也有覆蓋索引可用
[Index(nameof(FilePath), nameof(StartLine), nameof(StartColumn),
       nameof(EndLine), nameof(EndColumn))]
[Index(nameof(NodeId), IsUnique = true)]
[Index(nameof(NodeId), nameof(FilePath), nameof(StartLine),
       nameof(StartColumn), nameof(EndLine), nameof(EndColumn))]
public class DefineLocation : Location
{
}

// ──────────── Implementation ────────────
// ❶ 先用 NodeId，再帶座標 (JOIN + 座標查詢)
// ❷ 以 FilePath + 行區間供 GetNodeAndLine… 使用
[Index(nameof(NodeId), nameof(FilePath), nameof(StartLine),
       nameof(StartColumn), nameof(EndLine), nameof(EndColumn))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine))]
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
