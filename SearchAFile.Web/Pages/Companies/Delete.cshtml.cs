using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Companies;

public class DeleteModel : PageModel
{
    private readonly AuthenticatedApiClient _api;

    public DeleteModel(AuthenticatedApiClient api)
    {
        _api = api;
    }

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (id == null)
            return NotFound();

        var response = await _api.DeleteAsync($"companies/{id}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["MessageColor"] = "text-danger";
            TempData["Message"] = "Error deleting company.";

            return Page();
        }

        TempData["MessageColor"] = "text-success";
        TempData["Message"] = "Company successfully deleted.";

        return RedirectToPage("./Index");
    }
}
