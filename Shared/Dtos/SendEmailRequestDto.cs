using Shared.Enums;

namespace Shared.Dtos;

public class SendEmailRequestDto
{
    public string To { get; set; } = null!;
    public EmailTemplateType TemplateType { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
}