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
                id,
                company_id AS CompanyId,
                field_key  AS FieldKey,
                new_value  AS NewValue
            FROM company_change_requests
            WHERE id = @Id
        ", new { Id = requestId }, tx);

        switch (request.FieldKey)
        {
            // ================= COMPANY =================
            case "CompanyName":
                await UpdateCompany(conn, tx, request.CompanyId, "company_name", request.NewValue);
                break;

            case "CompanyWebsite":
                await UpdateCompany(conn, tx, request.CompanyId, "company_website", request.NewValue);
                break;

            case "BusinessType":
                await UpdateCompany(conn, tx, request.CompanyId, "business_type", request.NewValue);
                break;

            case "CompanySize":
                await UpdateCompany(conn, tx, request.CompanyId, "company_size", request.NewValue);
                break;

            case "YearEstablished":
                await UpdateCompany(conn, tx, request.CompanyId, "year_established", request.NewValue);
                break;

            case "CompanyOverview":
                await UpdateCompany(conn, tx, request.CompanyId, "company_overview", request.NewValue);
                break;

            // ================= ADDRESS =================
            case "AddressLine1":
                await UpdateAddress(conn, tx, request.CompanyId, "address_line1", request.NewValue);
                break;

            case "AddressLine2":
                await UpdateAddress(conn, tx, request.CompanyId, "address_line2", request.NewValue);
                break;

            case "City":
                await UpdateAddress(conn, tx, request.CompanyId, "city", request.NewValue);
                break;

            case "State":
                await UpdateAddress(conn, tx, request.CompanyId, "state", request.NewValue);
                break;

            case "PostalCode":
                await UpdateAddress(conn, tx, request.CompanyId, "postal_code", request.NewValue);
                break;

            case "Country":
                await UpdateAddress(conn, tx, request.CompanyId, "country", request.NewValue);
                break;

            // ================= PRIMARY CONTACT =================
            case "PrimaryContactName":
                await UpdateContact(conn, tx, request.CompanyId, "PRIMARY", "contact_name", request.NewValue);
                break;

            case "PrimaryContactRole":
                await UpdateContact(conn, tx, request.CompanyId, "PRIMARY", "role_designation", request.NewValue);
                break;

            case "PrimaryContactEmail":
                await UpdateContact(conn, tx, request.CompanyId, "PRIMARY", "email", request.NewValue);
                break;

            case "PrimaryContactPhone":
                await UpdateContact(conn, tx, request.CompanyId, "PRIMARY", "phone", request.NewValue);
                break;

            // ================= SECONDARY CONTACT =================
            case "SecondaryContactName":
                await UpdateContact(conn, tx, request.CompanyId, "SECONDARY", "contact_name", request.NewValue);
                break;

            case "SecondaryContactRole":
                await UpdateContact(conn, tx, request.CompanyId, "SECONDARY", "role_designation", request.NewValue);
                break;

            case "SecondaryContactEmail":
                await UpdateContact(conn, tx, request.CompanyId, "SECONDARY", "email", request.NewValue);
                break;

            case "SecondaryContactPhone":
                await UpdateContact(conn, tx, request.CompanyId, "SECONDARY", "phone", request.NewValue);
                break;

            // ================= CERTIFICATIONS =================
            case "Certifications":
                await ReplaceCertifications(conn, tx, request.CompanyId, request.NewValue);
                break;

            default:
                throw new InvalidOperationException($"Unsupported field key: {request.FieldKey}");
        }

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

   
   
private Task UpdateCompany(IDbConnection conn, IDbTransaction tx,
    Guid companyId, string column, string value)
{
    return conn.ExecuteAsync($@"
        UPDATE companies
        SET {column} = @Value,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @CompanyId
    ", new { Value = value, CompanyId = companyId }, tx);
}



private Task UpdateAddress(IDbConnection conn, IDbTransaction tx,
    Guid companyId, string column, string value)
{
    return conn.ExecuteAsync($@"
        UPDATE company_addresses
        SET {column} = @Value
        WHERE company_id = @CompanyId
    ", new { Value = value, CompanyId = companyId }, tx);
}



private Task UpdateContact(IDbConnection conn, IDbTransaction tx,
    Guid companyId, string type, string column, string value)
{
    return conn.ExecuteAsync($@"
        UPDATE company_contacts
        SET {column} = @Value
        WHERE company_id = @CompanyId
          AND contact_type = @Type
    ", new { Value = value, CompanyId = companyId, Type = type }, tx);
}



private async Task ReplaceCertifications(
    IDbConnection conn,
    IDbTransaction tx,
    Guid companyId,
    string value)
{
    await conn.ExecuteAsync(
        "DELETE FROM company_certifications WHERE company_id = @CompanyId",
        new { CompanyId = companyId }, tx);

    var certs = value.Split(',')
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x));

    foreach (var cert in certs)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO company_certifications (company_id, certification_name)
            VALUES (@CompanyId, @Name)
        ", new { CompanyId = companyId, Name = cert }, tx);
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
    
    
    public async Task<Guid?> GetCompanyIdByPrimaryEmailAsync(string email)
    {
        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<Guid?>(@"
        SELECT c.id
        FROM companies c
        INNER JOIN company_contacts cc
            ON cc.company_id = c.id
        WHERE cc.email = @Email
          AND cc.contact_type = 'PRIMARY'
    ", new { Email = email });
    }

    
    
    public async Task SaveResetTokenAsync(
        Guid companyId,
        string token,
        DateTime expiresAt)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
        UPDATE companies
        SET reset_password_token = @Token,
            reset_password_expires_at = @ExpiresAt
        WHERE id = @CompanyId
    ", new
        {
            CompanyId = companyId,
            Token = token,
            ExpiresAt = expiresAt
        });
    }

    
    public async Task<CompanyResetProjection?> GetByResetTokenAsync(string token)
    {
        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<CompanyResetProjection>(@"
        SELECT
            id AS CompanyId,
            reset_password_expires_at AS ExpiresAt
        FROM companies
        WHERE reset_password_token = @Token
    ", new { Token = token });
    }

    
    public async Task UpdatePasswordAsync(Guid companyId, string passwordHash)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
        UPDATE companies
        SET password_hash = @Hash,
            is_password_changed = true,
            reset_password_token = NULL,
            reset_password_expires_at = NULL
        WHERE id = @CompanyId
    ", new { Hash = passwordHash, CompanyId = companyId });
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
