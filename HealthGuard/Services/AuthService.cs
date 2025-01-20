using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealthGuard.Models;
using HealthGuard.Models.Auth;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using HealthGuard.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthGuard.Services;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginRequest request);
    Task<RegisterResponse> Register(RegisterRequest request);
    string GenerateJwtToken(User user, bool rememberMe = false);
    Task<bool> IsEmailExists(string email);
    Task DeleteAccount(int userId);
    Task EditProfile(int userId, EditProfileRequest request);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HealthDbContext _context;

    public AuthService(IConfiguration configuration, HealthDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        // Find the user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user, request.RememberMe);

        return new LoginResponse
        {
            Token = token,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = request.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7)
        };
    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        // Validate the request
        if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
            string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Email) ||
            !Enum.IsDefined(typeof(UserRole), request.Role))
        {
            throw new ArgumentException("Invalid registration request. All fields are required.");
        }

        // Check if the email already exists
        if (await IsEmailExists(request.Email))
        {
            throw new ArgumentException("Email already exists.");
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create the new user
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = request.Role,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log the inner exception for debugging
            Console.WriteLine($"DbUpdateException: {ex.InnerException?.Message}");
            throw new Exception("An error occurred while saving the user to the database.", ex);
        }

        // Return the RegisterResponse
        return new RegisterResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public string GenerateJwtToken(User user, bool rememberMe = false)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Email), // Use Email as the unique identifier
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FirstName", user.FirstName), // Add FirstName claim
                new Claim("LastName", user.LastName) // Add LastName claim
            }),
            Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> IsEmailExists(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task DeleteAccount(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("User not found.");

        // Delete the associated patient record (if any)
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient != null)
            _context.Patients.Remove(patient);

        // Delete the user
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task EditProfile(int userId, EditProfileRequest request)
    {
        // Find the user in the database
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("User not found.");

        // Update the user's profile information
        user.FirstName = request.FirstName ?? user.FirstName; // Update first name if provided
        user.LastName = request.LastName ?? user.LastName; // Update last name if provided
        user.Email = request.Email ?? user.Email; // Update email if provided

        // Save changes to the database
        await _context.SaveChangesAsync();
    }

    // Nested IEmailService interface
    public interface IEmailService
    {
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task SendEmailAsync(string to, string subject, string body);
        Task<string> GenerateVerificationCodeAsync(); // Add this method
    }

    // Nested EmailService class implementing IEmailService
    public class EmailService : IEmailService
    {
        private readonly Random _random = new Random();
        private readonly HealthDbContext _context;

        public EmailService(HealthDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateVerificationCodeAsync()
        {
            // Generate a 4-digit verification code
            return _random.Next(1000, 9999).ToString();
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            // Generate a 4-digit verification code
            var verificationCode = await GenerateVerificationCodeAsync();

            // Find the user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null; // User not found

            // Save the verification code to the user's record
            user.PasswordResetToken = verificationCode;
            await _context.SaveChangesAsync();

            return verificationCode;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            // Find the user by the verification code (token)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
            if (user == null)
                return false; // Invalid or expired token

            // Hash the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Clear the password reset token
            user.PasswordResetToken = null;

            // Save changes to the database
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Logic to send email (mock example)
            await Task.CompletedTask; // Replace with actual email sending
        }
    }

}