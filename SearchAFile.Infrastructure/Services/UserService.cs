using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Helpers;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly SearchAFileDbContext _context;

    public UserService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(string? search = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                (user.FullName != null && user.FullName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (user.FullNameReverse != null && user.FullNameReverse.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (user.EmailAddress != null && user.EmailAddress.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (user.PhoneNumber != null && user.PhoneNumber.Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id) => await _context.Users.FindAsync(id);

    public async Task CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<string?> EmailExistsAsync(Guid companyId, string email, Guid? userId = null)
    {
        return (await _context.Users
            .FirstOrDefaultAsync(u => userId == null ? true : u.UserId != userId
                && u.CompanyId == companyId
                && u.EmailAddress.Trim().ToLower().Equals(email.Trim().ToLower())))?.FullName;
    }
    public async Task<string?> PhoneExistsAsync(Guid companyId, string phone, Guid? userId = null)
    {
        return (await _context.Users
            .FirstOrDefaultAsync(u => (userId == null || u.UserId != userId)
                && u.CompanyId == companyId 
                && !string.IsNullOrEmpty(phone) 
                && u.PhoneNumber.Trim().ToLower().Equals(PhoneNumberHelper.CleanPhoneNumber(phone).Trim().ToLower())))?.FullName;
    }
}
