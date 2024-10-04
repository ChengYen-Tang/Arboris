using System.ComponentModel.DataAnnotations;

namespace Arboris.Service.Models;

public class CreateProjectRequest
{
    [Required]
    public IFormFile ProjectFile { get; set; }

    public string[]? ExcludePaths { get; set; }

    public IFormFile? ReportFile { get; set; }
}
