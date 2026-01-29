using FinancialManagementDataAccess.Models;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface IUserNotificationRepository
{
    /// <summary>
    /// Get all notifications visible to a supplier
    /// </summary>
    Task<IEnumerable<Notification>> GetNotificationsAsync(Guid supplierId);

    /// <summary>
    /// Get a single notification by id (only if supplier has access)
    /// </summary>
    Task<Notification?> GetByIdAsync(int id, Guid supplierId);

    /// <summary>
    /// Mark a notification as read for a supplier
    /// </summary>
    Task MarkAsReadAsync(int id, Guid supplierId);

    /// <summary>
    /// Get unread notification count for a supplier
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid supplierId);

    /// <summary>
    /// Get attachment (content, mime type, file name)
    /// </summary>
    Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id);
}