using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;

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
    public async Task<IActionResult> GetAll([FromQuery] string? search) => Ok(await _service.GetAllAsync(search));

    [HttpGet("{id}")]
    public async Task<ActionResult<Company>> GetById(Guid id)
    {
        var company = await _service.GetByIdAsync(id);
        if (company == null) return NotFound();
        return Ok(company);
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
