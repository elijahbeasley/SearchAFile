using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly SearchAFileDbContext _context;

    public CompanyService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Company>> GetAllAsync(string? search = null)
    {
        var query = _context.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(company =>
                (company.Company1 != null && company.Company1.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (company.ContactName != null && company.ContactName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (company.ContactEmailAddress != null && company.ContactEmailAddress.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (company.ContactPhoneNumber != null && company.ContactPhoneNumber.Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<Company?> GetByIdAsync(Guid id) => await _context.Companies.FindAsync(id);

    public async Task<Company?> CreateAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<bool> UpdateAsync(Company company)
    {
        _context.Companies.Update(company);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return false;

        _context.Companies.Remove(company);
        return await _context.SaveChangesAsync() > 0;
    }
}
