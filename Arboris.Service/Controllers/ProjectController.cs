using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.Models;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectController(ILogger<ProjectController> logger, IProjectRepository projectRepository, ClangFactory clangFactory, ProjectAggregate projectAggregate, CxxAggregate cxxAggregate) : ControllerBase
{
    private static readonly string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");

    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <param name="file"> Project file </param>
    /// <returns></returns>
    [HttpPost]
    [Route("Create")]
    [ProducesResponseType(typeof(OverallGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (!(file.ContentType.Contains("zip") || file.ContentType.Contains("ZIP")))
        {
            return BadRequest("Only .zip files are allowed.");
        }

        var allowedExtensions = new[] { ".zip" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Only .zip files are allowed.");
        }

        try
        {
            Result result = await projectRepository.CreateProjectAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string errorMessages = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}), Error message: {ErrorMessage}", errorId, id, errorMessages);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {errorMessages}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
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
                logger.LogWarning(ex, "Error Id: {ErrId}, ZipFile Failed", errorId);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {ex.Message}");
            }

            logger.LogError(ex, "Error Id: {ErrId}, ZipFile Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        using Clang clang = clangFactory.Create(id, projectDirectory);
        try
        {
            await clang.Scan();
        }
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(id);
            Directory.Delete(projectDirectory, true);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, clang.Scan Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        Directory.Delete(projectDirectory, true);
        try
        {
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
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(id);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, cxxAggregate.GetOverallGraphAsync Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <returns></returns>
    [HttpDelete]
    [Route("Delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            Result result = await projectRepository.DeleteProjectAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.DeleteProjectAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.DeleteProjectAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        return Ok();
    }

    [HttpGet]
    [Route("GetReport")]
    [ProducesResponseType(typeof(ProjectReport[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReport(Guid id)
    {
        Result<ProjectReport[]> result;
        try
        {
            result = await projectAggregate.GetReportAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.GetReportAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.GetReportAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        return Ok(result.Value);
    }
}
