using SendGrid;
using SendGrid.Helpers.Mail;

namespace HealthGuard.Services;

public interface INotificationService
{
    Task SendDiagnosisNotification(string email, string patientName, string diagnosisResult);
    Task SendDoctorAlert(string email, int patientId, string urgency);
}

public class NotificationService : INotificationService
{
    private readonly SendGridClient _client;
    private readonly IConfiguration _configuration;

    public NotificationService(IConfiguration configuration)
    {
        _configuration = configuration;
        var apiKey = _configuration["SendGrid:ApiKey"];
        Console.WriteLine($"SendGrid API Key: {apiKey}");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "SendGrid API Key is not configured.");
        }
        _client = new SendGridClient(apiKey);
    }


    public async Task SendDiagnosisNotification(string email, string patientName, string diagnosisResult)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_configuration["SendGrid:FromEmail"], "Health API"),
            Subject = "Diagnosis Results Available",
            PlainTextContent = $"Dear {patientName}, your diagnosis results are now available."
        };

        msg.AddTo(new EmailAddress(email));
        await _client.SendEmailAsync(msg);
    }

    public async Task SendDoctorAlert(string email, int patientId, string urgency)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_configuration["SendGrid:FromEmail"], "Health API"),
            Subject = $"Patient Review Required - Urgency: {urgency}",
            PlainTextContent = $"Patient ID {patientId} requires your immediate attention."
        };

        msg.AddTo(new EmailAddress(email));
        await _client.SendEmailAsync(msg);
    }
}