using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/collections")]
public class CollectionController : ControllerBase
{
    private readonly ICollectionService _service;

    public CollectionController(ICollectionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search)
    {
        try
        {
            var collections = await _service.GetAllAsync(search);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve collections.", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var collection = await _service.GetByIdAsync(id);

            if (collection == null)
                return NotFound();

            return Ok(collection);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve collection.", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Collection collection)
    {
        try
        {
            await _service.CreateAsync(collection);
            return CreatedAtAction(nameof(GetById), new { id = collection.CollectionId }, collection);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to create collection.", detail = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Collection collection)
    {
        try
        {
            if (id != collection.CollectionId) return BadRequest();

            var result = await _service.UpdateAsync(collection);
            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update collection.", detail = ex.Message });
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
            return StatusCode(500, new { message = "Failed to delete collection.", detail = ex.Message });
        }
    }
}
