using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Models.Auth;
using HealthGuard.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HealthGuard.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly HealthDbContext _context;

    public AuthController(IAuthService authService, HealthDbContext context) // Add HealthDbContext parameter
    {
        _authService = authService;
        _context = context; // Initialize _context with the injected parameter
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var response = await _authService.Login(request);

        if (response == null)
            return Unauthorized(new { message = "Invalid username or password" });

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(RegisterRequest request)
    {
        try
        {
            // Check if the username exists
            if (await _authService.IsUsernameExists(request.Username))
                return BadRequest(new { message = "Username already exists" });

            // Check if the email exists
            if (await _authService.IsEmailExists(request.Email))
                return BadRequest(new { message = "Email already exists" });

            // Register the user with the RegisterRequest
            var result = await _authService.Register(request); // Pass the whole request, not individual properties

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
   

    [HttpDelete("delete-account")]
    [Authorize] // Ensure only authenticated users can delete their account
    public async Task<IActionResult> DeleteAccount()
    {
        try
        {
            // Get the current user's ID from the claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Delete the user account using AuthService
            await _authService.DeleteAccount(userId);

            return Ok("Account deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPut("edit-profile")]
    [Authorize] // Ensure only authenticated users can edit their profile
    public async Task<IActionResult> EditProfile([FromBody] EditProfileRequest request)
    {
        try
        {
            // Debug: Check if the request object is null
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }

            // Debug: Check if the User object is null
            if (User == null)
            {
                return BadRequest("User object is null.");
            }

            // Debug: Check if the NameIdentifier claim is null
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return BadRequest("NameIdentifier claim is missing from the JWT token.");
            }

            // Debug: Print the userIdClaim value
            Console.WriteLine($"User ID Claim: {userIdClaim.Value}");

            // Get the current user's ID from the claims
            var userId = int.Parse(userIdClaim.Value);

            // Debug: Check if the _context object is null
            if (_context == null)
            {
                return StatusCode(500, "Database context is null.");
            }

            // Debug: Check if the user exists in the database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Debug: Print the user details
            Console.WriteLine($"User found: {user.Username}, {user.Email}");

            // Update the user's profile information
            user.Username = request.Username ?? user.Username; // Update username if provided
            user.Email = request.Email ?? user.Email; // Update email if provided

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok("Profile updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}