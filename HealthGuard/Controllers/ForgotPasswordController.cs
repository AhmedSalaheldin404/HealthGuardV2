using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Services;
using System.Threading.Tasks;
using static HealthGuard.Services.AuthService;

namespace HealthGuard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ForgotPasswordController : ControllerBase
{
    private readonly IEmailService _emailService;

    public ForgotPasswordController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // Request Password Reset with form-data
    [HttpPost("request-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromForm] ResetPasswordRequestModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request.");

        // Generate a 4-digit verification code
        var verificationCode = await _emailService.GeneratePasswordResetTokenAsync(model.Email);

        if (verificationCode == null)
            return NotFound("User not found.");

        // Send the verification code to the user's email
        var emailSubject = "Password Reset Verification Code";
        var emailBody = $"Your verification code is: {verificationCode}";
        await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

        // Return the verification code in the response (for testing purposes)
        return Ok(new
        {
            Message = "Password reset verification code has been sent to your email.",
            VerificationCode = verificationCode
        });
    }

    // Reset Password with form-data
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request.");

        var result = await _emailService.ResetPasswordAsync(model.Token, model.NewPassword);
        if (!result)
            return BadRequest("Invalid or expired token.");

        return Ok("Password has been reset successfully.");
    }
}
