using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Companies;

public class IndexModel : PageModel
{
    private readonly AuthenticatedApiClient _api;

    public IndexModel(AuthenticatedApiClient api)
    {
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public IList<Company>? Companies { get;set; } = default!;

    public async Task OnGetAsync()
    {
        string url = string.IsNullOrWhiteSpace(search)
            ? "companies"
            : $"companies?search={Uri.EscapeDataString(search)}";

        Companies = await _api.GetAsync<List<Company>>(url);
    }
}
