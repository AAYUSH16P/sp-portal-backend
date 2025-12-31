using DynamicFormService.DynamicFormServiceInterface;
using Shared.Dtos;

namespace Application.Services;

public class EmailService : IEmailService
{
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailSender _sender;

    public EmailService(
        ITemplateRenderer renderer,
        IEmailSender sender)
    {
        _renderer = renderer;
        _sender = sender;
    }

    public async Task SendAsync(SendEmailRequestDto request)
    {
        var body = _renderer.Render(
            request.TemplateType,
            request.Variables,
            out var subject);

        await _sender.SendAsync(
            request.To,
            subject,
            body);
    }
}