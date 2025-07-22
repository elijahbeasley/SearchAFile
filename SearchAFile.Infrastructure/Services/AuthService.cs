using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Data;

namespace SearchAFile.Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly SearchAFileDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(SearchAFileDbContext context, JwtTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);

        if (user == null)
            return new AuthResult { Success = false, ErrorMessage = "User not found" };

        if (!user.Active)
            return new AuthResult { Success = false, ErrorMessage = "User is inactive" };

        if (!user.EmailVerified)
            return new AuthResult { Success = false, ErrorMessage = "Your email address is not verified" };

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            return new AuthResult { Success = false, ErrorMessage = "Invalid password" };

        var token = _tokenGenerator.GenerateToken(user);

        return new AuthResult
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                UserId = user.UserId,
                EmailAddress = user.EmailAddress,
                FullName = user.FullName,
                Role = user.Role,
                Active = user.Active
            }
        };
    }
}

