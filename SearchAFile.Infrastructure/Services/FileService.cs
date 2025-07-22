using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly SearchAFileDbContext _context;

    public FileService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Core.Domain.Entities.File>> GetAllAsync(string? search = null)
    {
        var query = _context.Files.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(file =>
                (file.FileGroup != null && file.FileGroup.FileGroup1 != null && file.FileGroup.FileGroup1.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file.File1 != null && file.File1.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file.UploadedByUser != null && file.UploadedByUser.FullName != null && file.UploadedByUser.FullName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file.UploadedByUser != null && file.UploadedByUser.FullNameReverse != null && file.UploadedByUser.FullNameReverse.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (file.Uploaded != null && file.Uploaded.Value.ToString("dddd, M/d/yyyy h:mm tt").Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<Core.Domain.Entities.File?> GetByIdAsync(Guid id) => await _context.Files.FindAsync(id);

    public async Task CreateAsync(Core.Domain.Entities.File file)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Core.Domain.Entities.File file)
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
}
