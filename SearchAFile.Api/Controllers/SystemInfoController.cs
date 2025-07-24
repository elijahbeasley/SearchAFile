using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/systeminfos")]
public class SystemInfoController : ControllerBase
{
    private readonly ISystemInfoService _service;

    public SystemInfoController(ISystemInfoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var infos = await _service.GetAllAsync();
            return Ok(infos);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve system infos", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var info = await _service.GetByIdAsync(id);

            if (info == null)
                return NotFound();

            return Ok(info);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve system info", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemInfo info)
    {
        await _service.CreateAsync(info);
        return CreatedAtAction(nameof(GetById), new { id = info.SystemInfoId }, info);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SystemInfo info)
    {
        if (id != info.SystemInfoId) return BadRequest();
        return await _service.UpdateAsync(info) ? NoContent() : NotFound();
    }
}
