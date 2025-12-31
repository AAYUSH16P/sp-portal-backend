using Shared.Dtos;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface IEmailService
{
    Task SendAsync(SendEmailRequestDto request);

}