using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllAsync(string? search = null);
    Task<Event?> GetByIdAsync(Guid eventId);
}
