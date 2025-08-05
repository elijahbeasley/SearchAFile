using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class CollectionService : ICollectionService
{
    private readonly SearchAFileDbContext _context;

    public CollectionService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Collection>> GetAllAsync(string? search = null)
    {
        var query = _context.Collections.AsQueryable();

        search = search?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(collection =>
                (collection.Collection1 != null && collection.Collection1.Trim().ToLower().Contains(search)) ||
                (collection.CreatedByUser != null && collection.CreatedByUser.FirstName != null && collection.CreatedByUser.LastName != null && (collection.CreatedByUser.FirstName + " " + collection.CreatedByUser.LastName).Trim().ToLower().Contains(search)) ||
                (collection.CreatedByUser != null && collection.CreatedByUser.FirstName != null && collection.CreatedByUser.LastName != null && (collection.CreatedByUser.LastName + ", " + collection.CreatedByUser.FirstName).Trim().ToLower().Contains(search))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<Collection?> GetByIdAsync(Guid id) => await _context.Collections.FindAsync(id);

    public async Task<Collection?> CreateAsync(Collection collection)
    {
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        return collection;
    }

    public async Task<bool> UpdateAsync(Collection collection)
    {
        _context.Collections.Update(collection);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null) return false;

        _context.Collections.Remove(collection);
        return await _context.SaveChangesAsync() > 0;
    }
}
