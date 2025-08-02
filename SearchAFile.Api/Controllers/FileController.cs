using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IFileService _service;

    public FileController(IFileService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search)
    {
        try
        {
            var files = await _service.GetAllAsync(search);
            return Ok(files);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve files", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var file = await _service.GetByIdAsync(id);

            if (file == null)
                return NotFound();

            return Ok(file);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve file", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] File file)
    {
        try
        {
            await _service.CreateAsync(file);
            return CreatedAtAction(nameof(GetById), new { id = file.FileId }, file);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to create file.", detail = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] File file)
    {
        try
        {
            if (id != file.FileId) return BadRequest();

            var result = await _service.UpdateAsync(file);
            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update file.", detail = ex.Message });
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
            return StatusCode(500, new { message = "Failed to delete file.", detail = ex.Message });
        }
    }
}
