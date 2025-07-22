using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly SearchAFileDbContext _context;

    public EventService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync(string? search = null)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(_event =>
                (_event.TableName != null && _event.TableName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (_event.ActionType != null && _event.ActionType.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (_event.ChangedByUser != null && _event.ChangedByUser.FullName != null && _event.ChangedByUser.FullName.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (_event.ChangedByUser != null && _event.ChangedByUser.FullNameReverse != null && _event.ChangedByUser.FullNameReverse.Trim().ToLower().Contains(search.Trim().ToLower())) ||
                (_event.ChangeDate != null && _event.ChangeDate.Value.ToString("dddd, M/d/yyyy h:mm tt").Trim().ToLower().Contains(search.Trim().ToLower()))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(Guid id) => await _context.Events.FindAsync(id);
}
