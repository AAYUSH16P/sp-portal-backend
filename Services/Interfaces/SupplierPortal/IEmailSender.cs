namespace DynamicFormService.DynamicFormServiceInterface;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody,bool? status);

}