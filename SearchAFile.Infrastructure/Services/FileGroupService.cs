using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class FileGroupService : IFileGroupService
{
    private readonly SearchAFileDbContext _context;

    public FileGroupService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FileGroup>> GetAllAsync(string? search = null)
    {
        var query = _context.FileGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(file_group =>
                (file_group.FileGroup1 != null && file_group.FileGroup1.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file_group.CreatedByUser != null && file_group.CreatedByUser.FullName != null && file_group.CreatedByUser.FullName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file_group.CreatedByUser != null && file_group.CreatedByUser.FullNameReverse != null && file_group.CreatedByUser.FullNameReverse.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file_group.Created != null && file_group.Created.Value.ToString("dddd, M/d/yyyy h:mm tt").Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<FileGroup?> GetByIdAsync(Guid id) => await _context.FileGroups.FindAsync(id);

    public async Task CreateAsync(FileGroup group)
    {
        _context.FileGroups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(FileGroup group)
    {
        _context.FileGroups.Update(group);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var group = await _context.FileGroups.FindAsync(id);
        if (group == null) return false;

        _context.FileGroups.Remove(group);
        return await _context.SaveChangesAsync() > 0;
    }
}
