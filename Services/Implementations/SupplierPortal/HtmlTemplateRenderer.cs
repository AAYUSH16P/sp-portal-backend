using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.Extensions.Hosting;
using Shared.Enums;

namespace Infrastructure.Templates;

public class HtmlTemplateRenderer : ITemplateRenderer
{
    private readonly IHostEnvironment _env;

    public HtmlTemplateRenderer(IHostEnvironment env)
    {
        _env = env;
    }

    public string Render(
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        out string subject)
    {
        var fileName = templateType switch
        {
            EmailTemplateType.SupplierRejected => "SupplierRejected.html",
            EmailTemplateType.SupplierApproved => "SupplierApproved.html",
            EmailTemplateType.MeetingScheduled => "MeetingScheduled.html",
            EmailTemplateType.ForgotPassword   => "ForgotPassword.html", // ✅
            _ => throw new Exception("Invalid template")
        };

        subject = templateType switch
        {
            EmailTemplateType.SupplierRejected =>
                "Update on Supplier Registration – Westgate Supplier Onboarding Portal",

            EmailTemplateType.SupplierApproved =>
                "Access to Westgate Supplier Onboarding Portal & Supplier SLA",

            EmailTemplateType.MeetingScheduled =>
                "Meeting Scheduled – Microsoft Teams Invitation",

            EmailTemplateType.ForgotPassword =>
                "Reset Your Password – Westgate Supplier Portal", // ✅

            _ => string.Empty
        };

        var path = Path.Combine(
            _env.ContentRootPath,
            "EmailTemplates",
            fileName);

        var html = File.ReadAllText(path);

        foreach (var kv in variables)
        {
            html = html.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        }

        return html;
    }

}