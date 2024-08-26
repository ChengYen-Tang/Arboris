using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectController(ILogger<ProjectController> logger, IProjectRepository projectRepository, ClangFactory clangFactory, CxxAggregate cxxAggregate) : ControllerBase
{
    private static readonly string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");

    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <param name="file"> Project file </param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(OverallGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(Guid id, IFormFile file)
    {
        Result result = await projectRepository.CreateProjectAsync(id);
        if (result.IsFailed)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError("Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id})", errorId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        string projectDirectory = Path.Combine(cacheDirectory, id.ToString());
        if (Directory.Exists(projectDirectory))
            Directory.Delete(projectDirectory, true);
        Directory.CreateDirectory(projectDirectory);
        try
        {
            ZipFile.ExtractToDirectory(file.OpenReadStream(), projectDirectory);
        }
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(id);
            Directory.Delete(projectDirectory, true);
            Guid errorId = Guid.NewGuid();
            if (ex is UnauthorizedAccessException or NotSupportedException or InvalidDataException)
            {
                logger.LogError(ex, "Error Id: {ErrId}, ZipFile Failed", errorId);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {ex.Message}");
            }
            else
            {
                logger.LogError(ex, "Error Id: {ErrId}, ZipFile Failed", errorId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
            }
        }

        using Clang clang = clangFactory.Create(id, projectDirectory);
        await clang.Scan();
        Directory.Delete(projectDirectory, true);
        Result<OverallGraph> overallGraph = await cxxAggregate.GetOverallGraphAsync(id);
        if (overallGraph.IsFailed)
        {
            _ = await projectRepository.DeleteProjectAsync(id);
            Guid errorId = Guid.NewGuid();
            logger.LogError("Error Id: {ErrId}, cxxAggregate.GetOverallGraphAsync Failed, Error message: {Message}", errorId, string.Join(',', overallGraph.Errors.Select(item => item.Message)));
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        return Ok(overallGraph.Value);
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <returns></returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        Result result = await projectRepository.DeleteProjectAsync(id);
        if (result.IsFailed)
        {
            Guid errorId = Guid.NewGuid();
            string message = string.Join(',', result.Errors.Select(item => item.Message));
            logger.LogError("Error Id: {ErrId}, projectRepository.DeleteProjectAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
            return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
        }

        return Ok();
    }
}
