using SearchAFile.Core.Domain.Entities;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
}
