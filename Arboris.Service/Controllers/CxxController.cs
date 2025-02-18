using Arboris.Aggregate;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
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
    [Route("GetGraphForDescription")]
    [ProducesResponseType(typeof(ForDescriptionNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGraphForDescription(Guid id)
    {
        try
        {
            Result<ForDescriptionGraph> result = await cxxAggregate.GetGraphForDescription(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetGraphForDescription({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetGraphForDescription({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetGraphForUnitTest")]
    [ProducesResponseType(typeof(ForUnitTestGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGraphForUnitTest(Guid id)
    {
        try
        {
            Result<ForUnitTestGraph> result = await cxxAggregate.GetGraphForUnitTest(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetGraphForUnitTest({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetGraphForUnitTest({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetFuncInfoForUtService")]
    [ProducesResponseType(typeof(ForUtServiceFuncInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFuncInfoForUtService(Guid id)
    {
        try
        {
            Result<ForUtServiceFuncInfo> result = await cxxAggregate.GetFuncInfoForUtService(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetFuncInfoForUtService({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetFuncInfoForUtService({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetForCompile")]
    [ProducesResponseType(typeof(ForCompileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetForCompile(Guid id)
    {
        try
        {
            Result<ForCompileDto> result = await cxxAggregate.GetForCompileDtoAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetForCompile({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetForCompile({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetForGenerateCode")]
    [ProducesResponseType(typeof(ForGenerateCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetForGenerateCode(Guid id)
    {
        try
        {
            Result<ForGenerateCodeDto> result = await cxxAggregate.GetForGenerateCodeDtoAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetForGenerateCode({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetForGenerateCode({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
    }

    [HttpGet]
    [Route("GetNodeOtherInfo")]
    [ProducesResponseType(typeof(NodeOtherInfoWithLocation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNodeOtherInfo(Guid id)
    {
        try
        {
            Result<NodeOtherInfoWithLocation> result = await cxxAggregate.GetNodeOtherInfoAsync(id);
            if (result.IsFailed)
            {
                Guid errorId = Guid.NewGuid();
                string message = string.Join(',', result.Errors.Select(item => item.Message));
                logger.LogWarning("Error Id: {ErrId}, cxxAggregate.GetNodeOtherInfoAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
                return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
            }
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Guid errorId = Guid.NewGuid();
            logger.LogError(ex, "Error in cxxAggregate.GetNodeOtherInfoAsync({Id})", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error Id: {errorId}");
        }
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
}
