using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync(string? search = null);
    Task<User?> GetByIdAsync(Guid userId);
    Task CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid userId);
    Task<string?> EmailExistsAsync(Guid ComapnyId, string email, Guid? UserId);
    Task<string?> PhoneExistsAsync(Guid ComapnyId, string phone, Guid? UserId);
}
