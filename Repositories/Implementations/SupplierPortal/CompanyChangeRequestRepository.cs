using System.Data;
using Dapper;               // 🔥 REQUIRED
using Npgsql;
using DynamicFormRepo.DynamicFormRepoInterface;
using Shared;
using System.Data.Common;
using FinancialManagementDataAccess.Models;


public class CompanyChangeRequestRepository : ICompanyChangeRequestRepository
{
    private readonly string _connectionString;

    public CompanyChangeRequestRepository(IDbConnection db)
    {
        _connectionString = db.ConnectionString;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task CreateAsync(CompanyChangeRequestDto dto)
    {
        using var conn = CreateConnection();
        conn.Open();

        await conn.ExecuteAsync(@"
        INSERT INTO company_change_requests
        (
            company_id,
            field_key,
            old_value,
            new_value,
            reason,
            status,
            requested_at
        )
        VALUES
        (
            @CompanyId,
            @FieldKey,
            @OldValue,
            @NewValue,
            @Reason,
            'PENDING',
            CURRENT_TIMESTAMP
        )
    ", dto);
    }


    public async Task<List<CompanyChangeRequestViewDto>> GetPendingAsync()
    {
        using var conn = CreateConnection();
        conn.Open();
        var result = await conn.QueryAsync<CompanyChangeRequestViewDto>(@"
            SELECT 
                r.id,
                r.company_id AS CompanyId,
                c.company_name AS CompanyName,
                r.field_key AS FieldName,
                r.old_value AS OldValue,
                r.new_value AS NewValue,
                r.reason,
                r.status,
                r.requested_at AS RequestedAt
            FROM company_change_requests r
            JOIN companies c ON c.id = r.company_id
            WHERE r.status = 'PENDING'
            ORDER BY r.requested_at DESC
        ");

        return result.ToList();
    }

    public async Task ApproveAsync(Guid requestId, Guid adminId)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var request = await conn.QuerySingleAsync<CompanyChangeRequestProjection>(@"
    SELECT 
        company_id AS CompanyId,
        field_key  AS FieldKey,
        new_value  AS NewValue
    FROM company_change_requests
    WHERE id = @Id
", new { Id = requestId }, tx);


            if (!FieldColumnMap.TryGetValue(request.FieldKey, out var columnName))
            {
                throw new InvalidOperationException("Invalid field key");
            }

            var sql = $@"
    UPDATE companies
    SET {columnName} = @Value,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = @CompanyId
";

            await conn.ExecuteAsync(sql, new
            {
                Value = request.NewValue,
                CompanyId = request.CompanyId
            }, tx);


            await conn.ExecuteAsync(@"
            UPDATE company_change_requests
            SET status = 'APPROVED',
                reviewed_by = @AdminId,
                reviewed_at = CURRENT_TIMESTAMP
            WHERE id = @Id
        ", new { Id = requestId, AdminId = adminId }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task RejectAsync(Guid requestId, string remark, Guid adminId)
    {
        using var conn = CreateConnection();
        conn.Open();
        
        await conn.ExecuteAsync(@"
            UPDATE company_change_requests
            SET status = 'REJECTED',
                admin_remark = @Remark,
                reviewed_by = @AdminId,
                reviewed_at = CURRENT_TIMESTAMP
            WHERE id = @Id
        ", new
        {
            Id = requestId,
            Remark = remark,
            AdminId = adminId
        });
    }
    
    
    
    private static readonly Dictionary<string, string> FieldColumnMap =
        new()
        {
            { "CompanyName", "company_name" },
            { "CompanyWebsite", "company_website" },
            { "BusinessType", "business_type" },
            { "CompanySize", "company_size" },
            { "YearEstablished", "year_established" },
            { "CompanyOverview", "company_overview" },
            { "DomainExpertise", "domain_expertise" },
            { "TotalProjectsExecuted", "total_projects_executed" }
        };

}
