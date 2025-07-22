using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;

public class SystemInfoService : ISystemInfoService
{
    private readonly SearchAFileDbContext _context;

    public SystemInfoService(SearchAFileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SystemInfo>> GetAllAsync() => await _context.SystemInfos.ToListAsync();


    public async Task<SystemInfo?> GetByIdAsync(Guid id) => await _context.SystemInfos.FindAsync(id);

    public async Task CreateAsync(SystemInfo info)
    {
        _context.SystemInfos.Add(info);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(SystemInfo info)
    {
        _context.SystemInfos.Update(info);
        return await _context.SaveChangesAsync() > 0;
    }
}
