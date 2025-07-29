using Arboris.Aggregate;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using Arboris.Service.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CxxController(ILogger<CxxController> logger, ICxxRepository cxxRepository, CxxAggregate cxxAggregate) : ControllerBase
{
    [HttpPut]
    [Route("UpdateDescription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDescription(Guid id, [FromForm] string description)
    {
        try
        {
            Result result = await cxxAggregate.UpdateLLMDescriptionAsync(id, description);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.UpdateLLMDescriptionAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.UpdateLLMDescriptionAsync({Id}, {Description})", id, description);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
        return Ok();
    }

    [HttpGet]
    [Route("GetNodeInfoWithDependency")]
    [ProducesResponseType(typeof(NodeInfoWithDependency), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNodeInfoWithDependency(Guid id)
    {
        try
        {
            Result<NodeInfoWithDependency> result = await cxxAggregate.GetNodeInfoWithDependencyAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetNodeInfoWithDependencyAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetNodeInfoWithDependencyAsync({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetAllNodes")]
    [ProducesResponseType(typeof(GetAllNodeDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllNodes(Guid projectId)
    {
        try
        {
            Result<GetAllNodeDto[]> result = await cxxRepository.GetAllNodeAsync(projectId);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxRepository.GetAllNodeAsync({Id}) Failed, Error message: {Message}", errorId, projectId, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxRepository.GetAllNodeAsync({Id})", projectId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetSourceCodeFromFilePath")]
    [ProducesResponseType(typeof(GetSourceCodeFromFilePathResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSourceCodeFromFilePath(Guid projectId, string filePath, int line)
    {
        try
        {
            Result<NodeLines> result = await cxxRepository.GetSourceCodeFromFilePath(projectId, filePath, line);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxRepository.GetNodeAndLineStringFromFile({projectId}, {filePath}, {line})", projectId, filePath, line, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }

            string fileContent = GetLineContent(result.Value.Code!, (int)result.Value.StartLine, line);

            return Ok(new GetSourceCodeFromFilePathResponse(result.Value.Id, fileContent, result.Value.Code!));
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxRepository.GetNodeAndLineStringFromFile({projectId}, {filePath}, {line})", projectId, filePath, line);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    private static string GetLineContent(string text, int startLine, int targetLine)
    {
        string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        int index = targetLine - startLine; // 0-base 的 offset
        return lines[index];
    }
}
