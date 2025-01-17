namespace HealthGuard.Models.Auth;

public class EditProfileRequest
{
    public string? Username { get; set; } // Optional: New username
    public string? Email { get; set; } // Optional: New email
}