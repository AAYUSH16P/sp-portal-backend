using Shared.Enums;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface ITemplateRenderer
{
    string Render(
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        out string subject);
    
    
}