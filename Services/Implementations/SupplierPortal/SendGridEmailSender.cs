using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using DynamicFormService.DynamicFormServiceInterface;

namespace Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration config)
    {
        _apiKey = config["SendGrid:ApiKey"]
                  ?? throw new Exception("SendGrid API Key missing");

        _fromEmail = config["SendGrid:FromEmail"]!;
        _fromName = config["SendGrid:FromName"]!;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email is missing");

        var client = new SendGridClient(_apiKey);

        var from = new EmailAddress(_fromEmail, _fromName);
        var toEmail = new EmailAddress(to.Trim());

        var msg = MailHelper.CreateSingleEmail(
            from,
            toEmail,
            subject,
            null,
            htmlBody
        );

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid email failed: {error}");
        }
    }

}