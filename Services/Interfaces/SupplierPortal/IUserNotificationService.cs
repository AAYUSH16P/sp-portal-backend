using FinancialManagementDataAccess.Models;

namespace Application.Interfaces;

public interface IUserNotificationService
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
    /// Download attachment of a notification
    /// </summary>
    Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id);
}