using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Services;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _service;

    public EventController(IEventService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var events = await _service.GetAllAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve events", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var _event = await _service.GetByIdAsync(id);

            if (_event == null)
                return NotFound();

            return Ok(_event);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve event", detail = ex.Message });
        }
    }
}
