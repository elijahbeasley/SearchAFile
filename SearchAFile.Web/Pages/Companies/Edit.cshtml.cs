using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Services;


namespace SearchAFile.Web.Pages.Companies;

public class EditModel : PageModel
{
    private readonly AuthenticatedApiClient _api;

    public EditModel(AuthenticatedApiClient api)
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var response = await _api.PutAsync($"companies/{Company.CompanyId}", Company);

        if (!response.IsSuccessStatusCode)
        {
            TempData["MessageColor"] = "text-danger";
            TempData["Message"] = "Error updating company.";

            return Page();
        }

        var result = await _api.GetAsync<Company>($"companies/{Company.CompanyId}");


        TempData["MessageColor"] = "text-success";
        TempData["Message"] = "Product successfully updated.";

        return RedirectToPage("./Index");
    }
}
