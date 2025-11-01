using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DataSenseAPI.Application.Queries.MessageChannel;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Message Channel management controller
/// </summary>
[ApiController]
[Route("api/v1/message-channels")]
[Authorize]
public class MessageChannelController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessageChannelController> _logger;

    public MessageChannelController(IMediator mediator, ILogger<MessageChannelController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all message channels
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MessageChannel>>> GetAllChannels()
    {
        try
        {
            var channels = await _mediator.Send(new GetAllMessageChannelsQuery());
            return Ok(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message channels");
            return StatusCode(500, new { error = "Failed to get message channels", message = ex.Message });
        }
    }
}

