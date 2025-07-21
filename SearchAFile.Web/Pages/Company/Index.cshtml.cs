using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SearchAFile.Web.Models;

namespace SearchAFile.Web.Pages.Company
{
    public class IndexModel : PageModel
    {
        private readonly SearchAFile.Web.Models.SearchAFileDbContext _context;

        public IndexModel(SearchAFile.Web.Models.SearchAFileDbContext context)
        {
            _context = context;
        }

        public IList<Company> Company { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Company = await _context.Companies.ToListAsync();
        }
    }
}
