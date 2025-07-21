using SearchAFile.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface ISystemInfoService
{
    Task<IEnumerable<SystemInfo>> GetAllAsync();
    Task<SystemInfo?> GetByIdAsync(Guid id);
    Task CreateAsync(SystemInfo systemInfo);
    Task<bool> UpdateAsync(SystemInfo systemInfo);
}
