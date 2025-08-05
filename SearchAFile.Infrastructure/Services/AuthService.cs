using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Helpers;
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
            return new AuthResult { Success = false, ErrorMessage = "User not found." };

        if (!user.Active)
            return new AuthResult { Success = false, ErrorMessage = "User is inactive." };

        if (!user.EmailVerified)
            return new AuthResult { Success = false, ErrorMessage = @"Your email address has not been verified. <a class='btn btn-link p-0 m-0 cus-no-box-shadow fs-6' href='VerifyEmailAddress?id=" + user.EmailVerificationUrl + "'>Click here</a> to resend the email address verification email." };

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            return new AuthResult { Success = false, ErrorMessage = "Invalid password." };

        string token;
        try
        {
            token = _tokenGenerator.GenerateToken(user);
        }
        catch (Exception ex)
        {
            // Optional: Log ex
            return new AuthResult { Success = false, ErrorMessage = "Failed to generate authentication token." };
        }

        return new AuthResult
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                UserId = user.UserId,
                CompanyId = user.CompanyId,
                EmailAddress = user.EmailAddress,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                HeadshotPath = user.HeadshotPath,
                Role = user.Role,
                Active = user.Active
            }
        };
    }
}

