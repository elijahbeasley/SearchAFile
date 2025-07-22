using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Services;


namespace SearchAFile.Web.Pages.Companies;

public class DetailsModel : PageModel
{
    private readonly AuthenticatedApiClient _api;

    public DetailsModel(AuthenticatedApiClient api)
    {
        _api = api;
    }

    public Company Company { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null)
            return NotFound();

        var result = await _api.GetAsync<Company>($"companies/{id}");

        if (result == null)
            return NotFound();

        Company = result;

        return Page();
    }
}
