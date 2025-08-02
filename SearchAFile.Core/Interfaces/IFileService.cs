using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Core.Interfaces;

public interface IFileService
{
    Task<IEnumerable<File>> GetAllAsync(string? search = null);
    Task<File?> GetByIdAsync(Guid fileId);
    Task<File?> CreateAsync(File file);
    Task<bool> UpdateAsync(File file);
    Task<bool> DeleteAsync(Guid fileId);
}
