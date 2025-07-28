using System.ComponentModel;

namespace SearchAFile.Core.Domain.Entities;

public class UserDto
{
    public required Guid UserId { get; set; }

    public required Guid CompanyId { get; set; }

    [DisplayName("First Name")]
    public required string FirstName { get; set; }

    [DisplayName("Last Name")]
    public required string LastName { get; set; }

    [DisplayName("Name")]
    public string? FullName { get; set; }

    [DisplayName("Email Address")]
    public required string EmailAddress { get; set; }

    [DisplayName("Phone Number")]
    public required string PhoneNumber { get; set; }

    [DisplayName("Headshot")]
    public string? HeadshotPath { get; set; }

    public string? Role { get; set; }

    [DisplayName("Status")]
    public required bool Active { get; set; }
}
