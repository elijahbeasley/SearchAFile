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

        search = search?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(file_group =>
                (file_group.FileGroup1 != null && file_group.FileGroup1.Trim().ToLower().Contains(search)) ||
                (file_group.CreatedByUser != null && file_group.CreatedByUser.FirstName != null && file_group.CreatedByUser.LastName != null && (file_group.CreatedByUser.FirstName + " " + file_group.CreatedByUser.LastName).Trim().ToLower().Contains(search)) ||
                (file_group.CreatedByUser != null && file_group.CreatedByUser.FirstName != null && file_group.CreatedByUser.LastName != null && (file_group.CreatedByUser.LastName + ", " + file_group.CreatedByUser.FirstName).Trim().ToLower().Contains(search))
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
