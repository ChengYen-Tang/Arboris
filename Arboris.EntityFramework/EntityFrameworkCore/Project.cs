using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore;

public class Project
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public DateTime CreateTime { get; set; }
    [Required]
    public string SolutionName { get; set; }

    public ICollection<Node>? CxxNodes { get; set; }

    public Project()
        => (CreateTime) = (DateTime.Now);
}
