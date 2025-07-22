namespace SearchAFile.Core.Domain.Entities; 

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
}
