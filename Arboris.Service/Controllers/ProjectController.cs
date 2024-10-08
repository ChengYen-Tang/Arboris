﻿using Arboris.Aggregate;
using Arboris.Analyze.CXX;
using Arboris.Domain;
using Arboris.Models;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using Arboris.Service.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectController(ILogger<ProjectController> logger, IProjectRepository projectRepository, Project project, ClangFactory clangFactory, ProjectAggregate projectAggregate, CxxAggregate cxxAggregate) : ControllerBase
{
    private static readonly string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");

    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="id"> Project id </param>
    /// <param name="projectFile"> Project file </param>
    /// <returns></returns>
    [HttpPost]
    [Route("Create")]
    [ProducesResponseType(typeof(OverallGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromForm] CreateProjectRequest request)
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

        if (request.ReportFile is not null && request.ProjectFile.Length > 0)
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

        try
        {
            Result result = await projectRepository.CreateProjectAsync(request.Id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string errorMessages = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}), Error message: {ErrorMessage}", errorId, request.Id, errorMessages);
                return StatusCode(StatusCodes.Status400BadRequest, $"Error Id: {errorId}, Error message: {errorMessages}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, projectRepository.CreateProjectAsync({Id}) Failed, Error message: {Message}", errorId, request.Id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        string projectCacheDirectory = Path.Combine(cacheDirectory, request.Id.ToString());
        if (Directory.Exists(projectCacheDirectory))
            Directory.Delete(projectCacheDirectory, true);
        Directory.CreateDirectory(projectCacheDirectory);
        string projectDirectory = Path.Combine(projectCacheDirectory, "Project");
        string repotDirectory = Path.Combine(projectCacheDirectory, "Report");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(repotDirectory);
        try
        {
            ZipFile.ExtractToDirectory(request.ProjectFile.OpenReadStream(), projectDirectory);
        }
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(request.Id);
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

        using Clang clang = clangFactory.Create(request.Id, projectDirectory, request.ExcludePaths);
        try
        {
            await clang.Scan();
        }
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(request.Id);
            Directory.Delete(projectDirectory, true);
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error Id: {ErrId}, clang.Scan Failed, Error message: {Message}", errorId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }

        if (request.ReportFile is not null && request.ProjectFile.Length > 0)
        {
            try
            {
                ZipFile.ExtractToDirectory(request.ReportFile.OpenReadStream(), repotDirectory);
                await project.SyncFromReport(request.Id, repotDirectory);
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

        Directory.Delete(projectCacheDirectory, true);
        try
        {
            Result<OverallGraph> overallGraph = await cxxAggregate.GetOverallGraphAsync(request.Id);
            if (overallGraph.IsFailed)
            {
                _ = await projectRepository.DeleteProjectAsync(request.Id);
                Guid errorId = Guid.NewGuid();
                logger.LogError("Error Id: {ErrId}, cxxAggregate.GetOverallGraphAsync Failed, Error message: {Message}", errorId, string.Join(',', overallGraph.Errors.Select(item => item.Message)));
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
            }

            return Ok(overallGraph.Value);
        }
        catch (Exception ex)
        {
            _ = await projectRepository.DeleteProjectAsync(request.Id);
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
