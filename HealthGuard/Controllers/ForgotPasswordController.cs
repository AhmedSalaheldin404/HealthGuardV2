using HealthGuard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static HealthGuard.Services.AuthService;

namespace HealthGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ForgotPasswordController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request.");

            // Logic to generate reset token and send email
            var token = await _emailService.GeneratePasswordResetTokenAsync(model.Email);
            if (string.IsNullOrEmpty(token))
                return NotFound("User not found.");

            var resetLink = Url.Action(nameof(ResetPassword), "ForgotPassword", new { token }, Request.Scheme);

            // Print the reset link for debugging purposes
            Console.WriteLine($"Generated Reset Link: {resetLink}");
            Console.WriteLine($"Generated Token: {token}");

            // Send the reset link via email
            await _emailService.SendEmailAsync(model.Email, "Password Reset", $"Reset your password using this link: {resetLink}");

            // Return the token and link in the response (for testing purposes only; remove token in production)
            return Ok(new { Message = "Password reset link has been sent to your email.", Token = token, ResetLink = resetLink });
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request.");

            var result = await _emailService.ResetPasswordAsync(model.Token, model.NewPassword);
            if (!result)
                return BadRequest("Invalid or expired token.");

            return Ok("Password has been reset successfully.");
        }
    }
}
