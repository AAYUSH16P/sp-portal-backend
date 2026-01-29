using FinancialManagementDataAccess.Models;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface IUserNotificationRepository
{
    Task<IEnumerable<Notification>> GetNotificationsAsync(int supplierId);
    Task<Notification> GetByIdAsync(int id, int supplierId);
    Task MarkAsReadAsync(int id, int supplierId);
    Task<int> GetUnreadCountAsync(int supplierId);
    Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id);
}
