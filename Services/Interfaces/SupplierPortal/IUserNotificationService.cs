using FinancialManagementDataAccess.Models;

namespace Application.Interfaces;

public interface IUserNotificationService
{
    Task<IEnumerable<Notification>> GetNotificationsAsync(int supplierId);
    Task<Notification> GetByIdAsync(int notificationId, int supplierId);
    Task MarkAsReadAsync(int notificationId, int supplierId);
    Task<int> GetUnreadCountAsync(int supplierId);
    Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id);
}