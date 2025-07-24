using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Services;

namespace SearchAFile.Api.Controllers;

[ApiController]
[Route("api/companies")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _service;

    public CompanyController(ICompanyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search)
    {
        try
        {
            var companies = await _service.GetAllAsync(search);
            return Ok(companies);
        }
        catch (Exception ex)
        {
            // Optional logging here
            return StatusCode(500, new { message = "Failed to retrieve companies", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var company = await _service.GetByIdAsync(id);

            if (company == null)
                return NotFound();

            return Ok(company);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve company", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Company company)
    {
        await _service.CreateAsync(company);
        return CreatedAtAction(nameof(GetById), new { id = company.CompanyId }, company);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Company company)
    {
        if (id != company.CompanyId) return BadRequest();

        var result = await _service.UpdateAsync(company);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound();

        return NoContent();
    }
}
