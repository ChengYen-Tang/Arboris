using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.Domain;
using Arboris.Models;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using Arboris.Service.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.Json;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectController(ILogger<ProjectController> logger, IProjectRepository projectRepository, Project project, ClangFactory clangFactory, ProjectAggregate projectAggregate, CxxAggregate cxxAggregate) : ControllerBase
{
    public static readonly string CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");

    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <param name="projectFile"> Project file </param>
    /// <returns></returns>
    [HttpPost]
    [Route("Create")]
    [ProducesResponseType(typeof(CreateProjectResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(Guid id, [FromForm] CreateProjectRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.ProjectFile == null || request.ProjectFile.Length == 0)
        {
            return BadRequest("projectFile: No file uploaded.");
        }

        if (!(request.ProjectFile.ContentType.Contains("zip") || request.ProjectFile.ContentType.Contains("ZIP")))
        {
            return BadRequest("projectFile: Only .zip files are allowed.");
        }

        var allowedExtensions = new[] { ".zip" };
        var extension = Path.GetExtension(request.ProjectFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("projectFile: Only .zip files are allowed.");
        }

        if (request.ReportFile is not null && request.ReportFile.Length > 0)
        {
            if (!(request.ReportFile.ContentType.Contains("zip") || request.ReportFile.ContentType.Contains("ZIP")))
            {
                return BadRequest("reportFile: Only .zip files are allowed.");
            }

            extension = Path.GetExtension(request.ReportFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("reportFile: Only .zip files are allowed.");
            }
        }

        string projectCacheDirectory = Path.Combine(CacheDirectory, id.ToString());
        if (Directory.Exists(projectCacheDirectory))
            Directory.Delete(projectCacheDirectory, true);
        Directory.CreateDirectory(projectCacheDirectory);
        string projectDirectory = Path.Combine(projectCacheDirectory, "Project");
        string repotDirectory = Path.Combine(projectCacheDirectory, "Report");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(repotDirectory);

        #region IsCancellationRequested
        if (ct.IsCancellationRequested)
        {
            await Cleanup(id, projectCacheDirectory);
            return StatusCode(StatusCodes.Status400BadRequest, "Request cancelled");
        }
        #endregion
        #region Unzip and save vs solution
        try
        {
            ZipFile.ExtractToDirectory(request.ProjectFile.OpenReadStream(), projectDirectory);
        }
        catch (Exception ex)
        {
            await Cleanup(id, projectCacheDirectory);
            Guid errorId = Guid.NewGuid();
            if (ex is UnauthorizedAccessException or NotSupportedException or InvalidDataException)
            {
                logger.LogWarning(ex, "Error Id: {ErrId}, ZipFile Failed", errorId);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {ex.Message}");
            }

            logger.LogError(ex, "Error Id: {ErrId}, ZipFile Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
        #endregion

        #region IsCancellationRequested
        if (ct.IsCancellationRequested)
        {
            await Cleanup(id, projectCacheDirectory);
            return StatusCode(StatusCodes.Status400BadRequest, "Request cancelled");
        }
        #endregion
        #region Parse config file
        string projectConfigPath = Path.Combine(projectDirectory, "Config.json");
        if (!System.IO.File.Exists(projectConfigPath))
        {
            await Cleanup(id, projectCacheDirectory);
            Guid errorId = Guid.NewGuid();
            logger.LogWarning("Error Id: {ErrId}, Config file not found: {ProjectConfigPath}", errorId, projectConfigPath);
            return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Config file not found: {projectConfigPath}");
        }

        ProjectConfig? projectConfig = null!;
        try
        {
            string projectConfigJson = await System.IO.File.ReadAllTextAsync(projectConfigPath);
            projectConfig = JsonSerializer.Deserialize<ProjectConfig>(projectConfigJson);
            if (projectConfig is null)
            {
                await Cleanup(id, projectCacheDirectory);
                Guid errorId = Guid.NewGuid();
                logger.LogWarning("Error Id: {ErrId}, Config file is empty: {ProjectConfigPath}", errorId, projectConfigPath);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Config file is empty: {projectConfigPath}");
            }

            Result result = await projectRepository.CreateProjectAsync(id, projectConfig.SolutionName);
            if (result.IsFailed)
            {
                await Cleanup(id, projectCacheDirectory);
                Guid errorId = Guid.NewGuid();
                string errorMessages = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}), Error message: {ErrorMessage}", errorId, id, errorMessages);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {errorMessages}");
            }
        }
        catch (Exception ex)
        {
            await Cleanup(id, projectCacheDirectory);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
        #endregion

        #region Clang analysis
        Clang clang = null!;
        try
        {
            foreach (ProjectInfo projectInfo in projectConfig.ProjectInfos)
            {
                #region IsCancellationRequested
                if (ct.IsCancellationRequested)
                    break;
                #endregion

                clang = clangFactory.Create(id, projectDirectory, projectInfo);
                await clang.Scan(ct);
                clang.Dispose();
                clang = null!;
            }
        }
        catch (Exception ex)
        {
            await Cleanup(id, projectCacheDirectory);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, clang.Scan Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
        finally
        {
            clang?.Dispose();
            GC.Collect();
        }
        #endregion

        #region IsCancellationRequested
        if (ct.IsCancellationRequested)
        {
            await Cleanup(id, projectCacheDirectory);
            return StatusCode(StatusCodes.Status400BadRequest, "Request cancelled");
        }
        #endregion

        #region Process report
        if (request.ReportFile is not null && request.ProjectFile.Length > 0)
        {
            try
            {
                ZipFile.ExtractToDirectory(request.ReportFile.OpenReadStream(), repotDirectory);
                await project.SyncFromReport(id, repotDirectory);
            }
            catch (Exception ex)
            {
                Guid errorId = Guid.NewGuid();
                if (ex is UnauthorizedAccessException or NotSupportedException or InvalidDataException)
                {
                    logger.LogWarning(ex, "Error Id: {ErrId}, ZipFile Failed", errorId);
                    return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {ex.Message}");
                }

                logger.LogError(ex, "Error Id: {ErrId}, Process report failed, Error message: {Message}", errorId, ex.Message);
            }
        }
        #endregion

        Directory.Delete(projectCacheDirectory, true);

        #region IsCancellationRequested
        if (ct.IsCancellationRequested)
        {
            await Cleanup(id, projectCacheDirectory);
            return StatusCode(StatusCodes.Status400BadRequest, "Request cancelled");
        }
        #endregion

        #region Return OverallGraph
        try
        {
            Result<OverallGraph> overallGraph = await cxxAggregate.GetOverallGraphAsync(id);
            if (overallGraph.IsFailed)
            {
                await Cleanup(id, projectCacheDirectory);
                Guid errorId = Guid.NewGuid();
                logger.LogError("Error Id: {ErrId}, cxxAggregate.GetOverallGraphAsync Failed, Error message: {Message}", errorId, string.Join(',', overallGraph.Errors.Select(item => item.Message)));
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
            }
            CreateProjectResult createProjectResult = new(projectConfig.SolutionName, overallGraph.Value);
            return Ok(createProjectResult);
        }
        catch (Exception ex)
        {
            await Cleanup(id, projectCacheDirectory);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, cxxAggregate.GetOverallGraphAsync Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
        #endregion
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <returns></returns>
    [HttpDelete]
    [Route("Delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
                int statusCode = StatusCodes.Status404NotFound;
                if (result.Errors.Any(item => item is ExceptionalError { Exception: InvalidOperationException }))
                    statusCode = StatusCodes.Status403Forbidden;
                return StatusCode(statusCode, $"Error Id: {errorId}, Message: {message}");
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
                logger.LogWarning("Error Id: {ErrId}, projectAggregate.GetReportAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectAggregate.GetReportAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("GetProjects")]
    [ProducesResponseType(typeof(GetProject[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProjects()
    {
        try
        {
            GetProject[] result = await projectRepository.GetProjectsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.GetProjectsAsync() Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpPost]
    [Route("UpdateLocked")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLocked(Guid id, [FromForm] bool IsLocked)
    {
        try
        {
            Result result = await projectRepository.UpdateLockedAsync(id, IsLocked);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.UpdateLockedAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.UpdateLockedAsync({Id}) Failed, Error message: {Message}", errorId, id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        return Ok();
    }

    private async Task Cleanup(Guid id, string projectCacheDirectory)
    {
        _ = await projectRepository.DeleteProjectAsync(id);
        if (Directory.Exists(projectCacheDirectory))
            Directory.Delete(projectCacheDirectory, true);
    }
}
