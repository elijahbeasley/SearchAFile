using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemInfoController : ControllerBase
{
    private readonly ISystemInfoService _service;

    public SystemInfoController(ISystemInfoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
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
