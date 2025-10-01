using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly SearchAFileDbContext _context;

    public FileService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<File>> GetAllAsync(string? search = null)
    {
        var query = _context.Files.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(file =>
                (file.File1 != null && file.File1.Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<File?> GetByIdAsync(Guid id) => await _context.Files.FindAsync(id);

    public async Task<File?> CreateAsync(File file)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<bool> UpdateAsync(File file)
    {
        _context.Files.Update(file);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var file = await _context.Files.FindAsync(id);
        if (file == null) return false;

        _context.Files.Remove(file);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> GetFilesCountAsync(Guid? id)
    {
        if (id == null)
            return await _context.Files.CountAsync();
        else
            return await _context.Files.CountAsync(file => file.CollectionId == id);
    }
}
