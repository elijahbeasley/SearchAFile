using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface IFileGroupService
{
    Task<IEnumerable<FileGroup>> GetAllAsync();
    Task<FileGroup?> GetByIdAsync(Guid fileGroupId);
    Task CreateAsync(FileGroup fileGroup);
    Task<bool> UpdateAsync(FileGroup fileGroup);
    Task<bool> DeleteAsync(Guid fileGroupId);
}
