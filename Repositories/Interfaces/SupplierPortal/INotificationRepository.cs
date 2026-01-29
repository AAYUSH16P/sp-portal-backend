using FinancialManagementDataAccess.Models;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface INotificationRepository
{
    Task<int> CreateAsync(Notification notification);
    Task AddTargetsAsync(int notificationId, List<Guid> supplierIds);
    Task SendAsync(int notificationId, Guid adminId);
    Task DeleteAsync(int notificationId, Guid adminId);
    Task<Notification?> GetByIdAsync(int notificationId);
    Task UpdateAsync(Notification notification);
    Task RemoveTargetsAsync(int notificationId);
}
