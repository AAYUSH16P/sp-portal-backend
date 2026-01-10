using System.Data;
using Dapper;
using DynamicFormRepo.DynamicFormRepoInterface;
using Npgsql;
using Shared;

namespace DynamicFormRepo.DynamicFormRepoImplementation;

public class CalendarRepo : ICalendarRepo
{
    private readonly string _connectionString;

    public CalendarRepo(IDbConnection db)
    {
        _connectionString = db.ConnectionString;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<DateTime?> GetLastSyncAsync(string adminEmail)
    {
        using var conn = CreateConnection();
        await ((NpgsqlConnection)conn).OpenAsync();

        return await conn.ExecuteScalarAsync<DateTime?>(
            @"SELECT MAX(synced_at)
              FROM admin_calendar_events
              WHERE admin_email = @adminEmail",
            new { adminEmail });
    }

    public async Task ReplaceAdminEventsAsync(
        string adminEmail,
        List<AdminCalendarEventDto> events)
    {
        using var conn = CreateConnection();
        await ((NpgsqlConnection)conn).OpenAsync();

        using var tx = conn.BeginTransaction();

        try
        {
            // 1️⃣ Delete old events
            await conn.ExecuteAsync(
                "DELETE FROM admin_calendar_events WHERE admin_email = @adminEmail",
                new { adminEmail },
                tx);

            // 2️⃣ Insert new events
            const string sql = @"
                INSERT INTO admin_calendar_events
                (admin_email, event_id, subject, start_utc, end_utc)
                VALUES
                (@AdminEmail, @EventId, @Subject, @StartUtc, @EndUtc);
            ";

            foreach (var e in events)
            {
                await conn.ExecuteAsync(sql, new
                {
                    AdminEmail = adminEmail,
                    e.EventId,
                    e.Subject,
                    e.StartUtc,
                    e.EndUtc
                }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
    
    
    
    
    public async Task<List<AdminCalendarEventDto>> GetAdminEventsAsync(
        string adminEmail,
        DateTime startUtc,
        DateTime endUtc)
    {
        using var conn = CreateConnection();

        const string sql = @"
        SELECT
            event_id   AS EventId,
            subject    AS Subject,
            start_utc  AS StartUtc,
            end_utc    AS EndUtc
        FROM admin_calendar_events
        WHERE admin_email = @adminEmail
          AND start_utc < @endUtc
          AND end_utc   > @startUtc
        ORDER BY start_utc;
    ";

        return (await conn.QueryAsync<AdminCalendarEventDto>(sql, new
        {
            adminEmail,
            startUtc,
            endUtc
        })).ToList();
    }
    
    
    public async Task UpdateNextMeetingAsync(Guid companyId, DateTime meetingUtc)
    {
        using var conn = CreateConnection();

        var sql = @"
        UPDATE companies
        SET next_meeting_at = @MeetingUtc,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @CompanyId;
    ";

        await conn.ExecuteAsync(sql, new
        {
            CompanyId = companyId,
            MeetingUtc = meetingUtc
        });
    }


}
