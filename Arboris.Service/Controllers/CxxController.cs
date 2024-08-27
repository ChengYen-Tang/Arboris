using Arboris.Aggregate;
using Arboris.Models.Graph.CXX;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Arboris.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CxxController(ILogger<CxxController> logger, CxxAggregate cxxAggregate) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDescription(Guid id, string description)
    {
        Result result = await cxxAggregate.UpdateLLMDescriptionAsync(id, description);
        if (result.IsFailed)
        {
            Guid errorId = Guid.NewGuid();
            string message = string.Join(',', result.Errors.Select(item => item.Message));
            logger.LogError("Error Id: {ErrId}, cxxAggregate.UpdateLLMDescriptionAsync({Id}) Failed, Error message: {Message}", errorId, id, message);
            return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
        }
        return Ok();
    }

    [HttpGet]
    [ProducesResponseType(typeof(ForDescriptionNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGraphForDescription(Guid id)
    {
        Result<ForDescriptionGraph> result = await cxxAggregate.GetGraphForDescription(id);
        if (result.IsFailed)
        {
            Guid errorId = Guid.NewGuid();
            string message = string.Join(',', result.Errors.Select(item => item.Message));
            logger.LogError("Error Id: {ErrId}, cxxAggregate.GetGraphForDescription({Id}) Failed, Error message: {Message}", errorId, id, message);
            return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
        }
        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ForUnitTestGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGraphForUnitTest(Guid id)
    {
        Result<ForUnitTestGraph> result = await cxxAggregate.GetGraphForUnitTest(id);
        if (result.IsFailed)
        {
            Guid errorId = Guid.NewGuid();
            string message = string.Join(',', result.Errors.Select(item => item.Message));
            logger.LogError("Error Id: {ErrId}, cxxAggregate.GetGraphForUnitTest({Id}) Failed, Error message: {Message}", errorId, id, message);
            return StatusCode(StatusCodes.Status404NotFound, $"Error Id: {errorId}, Message: {message}");
        }
        return Ok(result.Value);
    }
}
