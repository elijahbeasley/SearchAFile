using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface ICollectionService
{
    Task<IEnumerable<Collection>> GetAllAsync(string? search = null);
    Task<Collection?> GetByIdAsync(Guid collectionId);
    Task<Collection?> CreateAsync(Collection collection);
    Task<bool> UpdateAsync(Collection collection);
    Task<bool> DeleteAsync(Guid collectionId);
}
