using System.Data;
using Dapper;
using DynamicFormRepo.DynamicFormRepoInterface;
using FinancialManagementDataAccess.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Repositories;

public class UserNotificationRepository : IUserNotificationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<UserNotificationRepository> _logger;

    public UserNotificationRepository(
        IDbConnection db,
        ILogger<UserNotificationRepository> logger)
    {
        _connectionString = db.ConnectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(Guid supplierId)
    {
        const string sql = """
        SELECT
            n.notification_id AS "NotificationId",
            n.title            AS "Title",
            n.message          AS "Message",
            n.type             AS "Type",
            n.priority         AS "Priority",
            n.created_at       AS "CreatedAt",
            COALESCE(nr.is_read, false) AS "IsRead"
        FROM notifications n
        LEFT JOIN notification_targets nt
            ON nt.notification_id = n.notification_id
        LEFT JOIN notification_reads nr
            ON nr.notification_id = n.notification_id
           AND nr.supplier_id = @SupplierId
        WHERE n.status = 'SENT'
          AND (n.target_type = 'ALL' OR nt.supplier_id = @SupplierId)
        ORDER BY n.created_at DESC;
        """;

        using var conn = CreateConnection();
        return await conn.QueryAsync<Notification>(sql, new { SupplierId = supplierId });
    }

    public async Task<Notification?> GetByIdAsync(int id, Guid supplierId)
    {
        const string sql = """
        SELECT n.*
        FROM notifications n
        LEFT JOIN notification_targets nt
            ON nt.notification_id = n.notification_id
        WHERE n.notification_id = @Id
          AND n.status = 'SENT'
          AND (n.target_type = 'ALL' OR nt.supplier_id = @SupplierId);
        """;

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Notification>(
            sql,
            new { Id = id, SupplierId = supplierId });
    }

    public async Task MarkAsReadAsync(int id, Guid supplierId)
    {
        const string sql = """
        INSERT INTO notification_reads
            (notification_id, supplier_id, is_read, read_at)
        VALUES
            (@Id, @SupplierId, true, NOW())
        ON CONFLICT (notification_id, supplier_id)
        DO UPDATE
            SET is_read = true,
                read_at = NOW();
        """;

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id, SupplierId = supplierId });
    }

    public async Task<int> GetUnreadCountAsync(Guid supplierId)
    {
        const string sql = """
        SELECT COUNT(*)
        FROM notifications n
        LEFT JOIN notification_reads nr
            ON nr.notification_id = n.notification_id
           AND nr.supplier_id = @SupplierId
        LEFT JOIN notification_targets nt
            ON nt.notification_id = n.notification_id
        WHERE n.status = 'SENT'
          AND (n.target_type = 'ALL' OR nt.supplier_id = @SupplierId)
          AND COALESCE(nr.is_read, false) = false;
        """;

        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { SupplierId = supplierId });
    }

    public async Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id)
    {
        const string sql = """
        SELECT attachment_content, attachment_mime, attachment_name
        FROM notifications
        WHERE notification_id = @Id;
        """;

        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<(byte[], string, string)>(
            sql,
            new { Id = id });
    }
}
