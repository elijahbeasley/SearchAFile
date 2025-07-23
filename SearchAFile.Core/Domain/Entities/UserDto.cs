namespace SearchAFile.Core.Domain.Entities;

public class UserDto
{
    public required Guid UserId { get; set; }
    public required string EmailAddress { get; set; }
    public required string FullName { get; set; }
    public string? HeadshotPath { get; set; }
    public required string Role { get; set; }
    public required bool Active { get; set; }
}
