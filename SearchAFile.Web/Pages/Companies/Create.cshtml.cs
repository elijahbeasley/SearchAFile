using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;


namespace SearchAFile.Web.Pages.Companies;

public class CreateModel : PageModel
{
    private readonly AuthenticatedApiClient _api;

    public CreateModel(AuthenticatedApiClient api)
    {
        _api = api;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public Company Company { get; set; } = default!;

    public async Task<IActionResult> OnPostAsync()
    {
        var response = await _api.PostAsync("companies", Company);

        if (!response.IsSuccessStatusCode)
        {
            await ApiErrorHelper.AddErrorsToModelStateAsync(response, ModelState, "Company");
            return Page();
        }
        TempData["MessageColor"] = "text-success";
        TempData["Message"] = "Product created successfully!";
        return RedirectToPage("./Index");
    }
}
