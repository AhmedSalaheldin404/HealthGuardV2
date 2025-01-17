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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
}


