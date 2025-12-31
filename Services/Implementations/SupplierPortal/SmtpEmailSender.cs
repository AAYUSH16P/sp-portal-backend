using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using DynamicFormService.DynamicFormServiceInterface;

namespace Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("SmtpSettings");

        using var client = new SmtpClient(
            smtp["Host"],
            int.Parse(smtp["Port"]!)
        )
        {
            Credentials = new NetworkCredential(
                smtp["SenderEmail"],
                smtp["SenderPassword"]
            ),
            EnableSsl = bool.Parse(smtp["EnableSsl"]!)
        };

        var mail = new MailMessage
        {
            From = new MailAddress(
                smtp["SenderEmail"]!,
                smtp["SenderName"]
            ),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(to);

        await client.SendMailAsync(mail);
    }
}