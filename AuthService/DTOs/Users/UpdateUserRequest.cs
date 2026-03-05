namespace AuthService.DTOs.Users;

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "User";
}
