using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Core.Interfaces
{
    public interface ICompanyService
    {
        Task<IEnumerable<Company>> GetAllAsync(string? search = null);
        Task<Company?> GetByIdAsync(Guid id);
        Task CreateAsync(Company company);
        Task<bool> UpdateAsync(Company company);
        Task<bool> DeleteAsync(Guid id);
    }
}
