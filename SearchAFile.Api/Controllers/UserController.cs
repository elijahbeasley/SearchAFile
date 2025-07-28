using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Services;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search)
    {
        try
        {
            var users = await _service.GetAllAsync(search);
            return Ok(users);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve users", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var user = await _service.GetByIdAsync(id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve user", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        await _service.CreateAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] User user)
    {
        try
        {
            if (id != user.UserId) return BadRequest();
            return await _service.UpdateAsync(user) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update user", detail = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            return await _service.DeleteAsync(id) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete user", detail = ex.Message });
        }
    }

    [HttpGet("emailexists")]
    public async Task<ActionResult<string?>> EmailExists(Guid companyId, string email, Guid? userId = null)
    {
        try
        {
            var exists = await _service.EmailExistsAsync(companyId, email, userId);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to check user email address exists", detail = ex.Message });
        }
    }

    [HttpGet("phoneexists")]
    public async Task<ActionResult<string?>> PhoneExists(Guid companyId, string phone, Guid? userId = null)
    {
        try
        {
            var exists = await _service.PhoneExistsAsync(companyId, phone, userId);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to check phone number exists", detail = ex.Message });
        }
    }
}
