using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IFileService _service;

    public FileController(IFileService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var files = await _service.GetAllAsync();
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
    public async Task<IActionResult> Create([FromBody] Core.Domain.Entities.File file)
    {
        await _service.CreateAsync(file);
        return CreatedAtAction(nameof(GetById), new { id = file.FileId }, file);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Core.Domain.Entities.File file)
    {
        if (id != file.FileId) return BadRequest();
        return await _service.UpdateAsync(file) ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
