using Shared.Dtos;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface INotificationService
{
    Task CreateAsync(CreateNotificationDto dto, Guid adminId);
    Task UpdateAsync(UpdateNotificationDto dto, Guid adminId);

    // Optional but recommended
    Task SendAsync(int notificationId, Guid adminId);
    Task DeleteAsync(int notificationId, Guid adminId);
}