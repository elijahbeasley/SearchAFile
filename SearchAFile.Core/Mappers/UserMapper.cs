using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Core.Mappers;
public static class DtoMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto
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
        };
    }
}
