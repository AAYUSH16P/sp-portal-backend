using System.Data;
using DynamicFormRepo.DynamicFormRepoInterface;
using FinancialManagementDataAccess.Models;
using Dapper;

namespace DynamicFormRepo.DynamicFormRepoImplementation;

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnection _db;

    public NotificationRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(Notification n)
    {
        var sql = @"
        INSERT INTO notifications
        (title, message, type, priority, target_type, status,
         attachment_name, attachment_content, attachment_mime,
         created_by_admin_id)
        VALUES
        (@Title, @Message, @Type, @Priority, @TargetType, 'DRAFT',
         @AttachmentName, @AttachmentContent, @AttachmentMime,
         @CreatedByAdminId)
        RETURNING notification_id;
        ";

        return await _db.ExecuteScalarAsync<int>(sql, n);
    }

    public async Task AddTargetsAsync(int notificationId, List<int> supplierIds)
    {
        var sql = @"
        INSERT INTO notification_targets (notification_id, supplier_id)
        VALUES (@NotificationId, @SupplierId);
        ";

        foreach (var id in supplierIds)
        {
            await _db.ExecuteAsync(sql, new
            {
                NotificationId = notificationId,
                SupplierId = id
            });
        }
    }

    public async Task SendAsync(int id, int adminId)
    {
        await _db.ExecuteAsync(@"
        UPDATE notifications
        SET status = 'SENT',
            sent_by_admin_id = @AdminId,
            sent_at = NOW()
        WHERE notification_id = @Id", new { Id = id, AdminId = adminId });
    }

    public async Task DeleteAsync(int id, int adminId)
    {
        await _db.ExecuteAsync(@"
        UPDATE notifications
        SET status = 'DELETED',
            is_deleted = TRUE,
            deleted_by_admin_id = @AdminId,
            deleted_at = NOW()
        WHERE notification_id = @Id", new { Id = id, AdminId = adminId });
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _db.QueryFirstOrDefaultAsync<Notification>(
            "SELECT * FROM notifications WHERE notification_id = @Id",
            new { Id = id });
    }
    
    
    public async Task UpdateAsync(Notification n)
    {
        var sql = @"
    UPDATE notifications
    SET
        title = @Title,
        message = @Message,
        type = @Type,
        priority = @Priority,
        target_type = @TargetType,

        attachment_name = @AttachmentName,
        attachment_content = @AttachmentContent,
        attachment_mime = @AttachmentMime,

        updated_at = NOW()

    WHERE notification_id = @NotificationId
      AND status = 'DRAFT'
      AND is_deleted = FALSE;
    ";

        await _db.ExecuteAsync(sql, n);
    }

    public async Task RemoveTargetsAsync(int notificationId)
    {
        await _db.ExecuteAsync(
            "DELETE FROM notification_targets WHERE notification_id = @Id",
            new { Id = notificationId }
        );
    }
}

