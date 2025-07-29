using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/filegroups")]
public class FileGroupController : ControllerBase
{
    private readonly IFileGroupService _service;

    public FileGroupController(IFileGroupService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search)
    {
        try
        {
            var groups = await _service.GetAllAsync(search);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve file groups.", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var group = await _service.GetByIdAsync(id);

            if (group == null)
                return NotFound();

            return Ok(group);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve file group.", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FileGroup group)
    {
        try
        {
            await _service.CreateAsync(group);
            return CreatedAtAction(nameof(GetById), new { id = group.FileGroupId }, group);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to create file group.", detail = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FileGroup group)
    {
        try
        {
            if (id != group.FileGroupId) return BadRequest();

            var result = await _service.UpdateAsync(group);
            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update file group.", detail = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete file group.", detail = ex.Message });
        }
    }
}
