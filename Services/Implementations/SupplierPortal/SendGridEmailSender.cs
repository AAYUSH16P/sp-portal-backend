using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
   private readonly  ILogger _logger;
    public SendGridEmailSender(IConfiguration config,ILogger<SendGridEmailSender> logger)
    {
        _apiKey = config["SendGrid:ApiKey"]
                  ?? throw new Exception("SendGrid API Key missing");

        _fromEmail = config["SendGrid:FromEmail"]!;
        _fromName = config["SendGrid:FromName"]!;
        _logger=logger;
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
            plainTextContent: null,
            htmlContent: htmlBody
        );


        var slaPath = Path.Combine(
            AppContext.BaseDirectory,
            "EmailTemplates",
            "Attachments",
            "SLA - Talented Staff.pdf"
        );
        
        _logger.LogInformation("SLA PDF path resolved to: {Path}", slaPath);



        if (!File.Exists(slaPath))
        {
            throw new FileNotFoundException("SLA PDF not found", slaPath);
        }

        var pdfBytes = await File.ReadAllBytesAsync(slaPath);
        var pdfBase64 = Convert.ToBase64String(pdfBytes);

        msg.AddAttachment(
            "Supplier_SLA.pdf",        // attachment name shown to user
            pdfBase64,
            "application/pdf"
        );

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid email failed: {error}");
        }
    }

}