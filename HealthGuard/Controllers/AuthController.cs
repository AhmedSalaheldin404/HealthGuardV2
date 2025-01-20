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

    public AuthController(IAuthService authService, HealthDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    // Login with form-data
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromForm] LoginRequest request)
    {
        var response = await _authService.Login(request);

        if (response == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(response);
    }

    // Register with form-data
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromForm] RegisterRequest request)
    {
        try
        {
            // Check if the email exists
            if (await _authService.IsEmailExists(request.Email))
                return BadRequest(new { message = "Email already exists" });

            // Register the user
            var result = await _authService.Register(request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while registering the user.", details = ex.Message });
        }
    }

    // Delete account
    [HttpDelete("delete-account")]
    [Authorize]
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

    // Edit profile with form-data
    [HttpPut("edit-profile")]
    [Authorize]
    public async Task<IActionResult> EditProfile([FromForm] EditProfileRequest request)
    {
        try
        {
            // Get the current user's ID from the claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Update the user's profile information
            await _authService.EditProfile(userId, request);

            return Ok("Profile updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}