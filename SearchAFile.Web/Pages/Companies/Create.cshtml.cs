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
        // Set the page title.
        HttpContext.Session.SetString("PageTitle", "Create Company");

        return Page();
    }

    [BindProperty]
    public Company Company { get; set; } = default!;

    public async Task<IActionResult> OnPostAsync()
    {
        //if (!ModelState.IsValid)
        //{
        //    return Page();
        //}
        //var response = await _api.PostAsync("companies", Company);

        //if (!response.IsSuccessStatusCode)
        //{
        //    await ApiErrorHelper.AddErrorsToModelStateAsync(response, ModelState, "Company");
        //    return Page();
        //}

        var result = await _api.PostAsync<Company>("companies", Company);

        if (!result.IsSuccess)
        {
            bool boo = ModelState.IsValid;
            await ApiErrorHelper.AddErrorsToModelStateAsync(result, ModelState, "Company");
            boo = ModelState.IsValid;
            return Page();
        }

        TempData["StartupJavaScript"] = "ShowSnack('success', 'Product successfully created', 7000, true)";

        return RedirectToPage("./Index");
    }
}
