using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface IFileService
{
    Task<IEnumerable<Domain.Entities.File>> GetAllAsync();
    Task<Domain.Entities.File?> GetByIdAsync(Guid fileId);
    Task CreateAsync(Domain.Entities.File file);
    Task<bool> UpdateAsync(Domain.Entities.File file);
    Task<bool> DeleteAsync(Guid fileId);
}
