namespace HealthGuard.Models.Auth;

public class EditProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}