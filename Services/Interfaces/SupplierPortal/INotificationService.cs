using Shared.Dtos;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface INotificationService
{
    Task CreateAsync(CreateNotificationDto dto, int adminId);
    Task UpdateAsync(UpdateNotificationDto dto, int adminId);

    // Optional but recommended
    Task SendAsync(int notificationId, int adminId);
    Task DeleteAsync(int notificationId, int adminId);
}