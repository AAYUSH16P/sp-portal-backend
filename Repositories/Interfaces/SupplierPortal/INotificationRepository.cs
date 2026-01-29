using FinancialManagementDataAccess.Models;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface INotificationRepository
{
    Task<int> CreateAsync(Notification notification);
    Task AddTargetsAsync(int notificationId, List<int> supplierIds);
    Task SendAsync(int notificationId, int adminId);
    Task DeleteAsync(int notificationId, int adminId);
    Task<Notification?> GetByIdAsync(int notificationId);
    Task UpdateAsync(Notification notification);
    Task RemoveTargetsAsync(int notificationId);
}
