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
    Task<RegisterResponse> Register(RegisterRequest request); // Corrected method signature
    string GenerateJwtToken(User user);

    Task<bool> IsUsernameExists(string username);
    Task<bool> IsEmailExists(string email);
    Task DeleteAccount(int userId);
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        // Check if the username exists
        if (await IsUsernameExists(request.Username))
            throw new Exception("Username already exists");

        // Check if the email exists
        if (await IsEmailExists(request.Email))
            throw new Exception("Email already exists");

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create the new user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            Role = request.Role,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Return the UserResponse without the PasswordHash
        return new RegisterResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }


    public string GenerateJwtToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> IsUsernameExists(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailExists(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public interface IEmailService
    {
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            // Logic to generate token (mock example)
            return "mock-token"; // Replace with actual token generation logic
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            // Logic to validate token and reset password (mock example)
            return token == "mock-token"; // Replace with actual validation
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Logic to send email (mock example)
            await Task.CompletedTask; // Replace with actual email sending
        }
    }


    public async Task DeleteAccount(int userId) // Implement the DeleteAccount method
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
}
