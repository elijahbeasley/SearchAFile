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

    public async Task<IEnumerable<Event>> GetAllAsync() => await _context.Events.ToListAsync();

    public async Task<Event?> GetByIdAsync(Guid id) => await _context.Events.FindAsync(id);
}
