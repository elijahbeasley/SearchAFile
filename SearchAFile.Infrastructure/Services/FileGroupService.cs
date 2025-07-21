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

    public async Task<IEnumerable<FileGroup>> GetAllAsync() => await _context.FileGroups.ToListAsync();

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
