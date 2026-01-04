using Dapper;
using Npgsql;
using System.Data;
using DynamicFormRepo.DynamicFormRepoInterface;
using FinancialManagementDataAccess.Models;
using Shared;
using Shared.Dtos;

namespace DynamicFormRepo.DynamicFormRepoImplementation;

public class CompanyApprovalRepo : ICompanyApprovalRepo
{
    private readonly string _connectionString;

    public CompanyApprovalRepo(IDbConnection db)
    {
        _connectionString = db.ConnectionString;
    }

    private IDbConnection CreateConnection()
        => new NpgsqlConnection(_connectionString);

    public async Task ApproveCompanyAsync(Guid companyId, string passwordHash)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(
            @"UPDATE companies
          SET isapproved = TRUE,
              remark = NULL,
              password_hash = @passwordHash,
              updated_at = NOW()
          WHERE id = @companyId",
            new { companyId, passwordHash });
    }


    public async Task RejectCompanyAsync(Guid companyId, string remark)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE companies
              SET isapproved = FALSE,
                  remark = @remark,
                  updated_at = NOW()
              WHERE id = @companyId",
            new { companyId, remark });
    }

    public async Task<(string Email, string ContactName)?> GetPrimaryContactAsync(Guid companyId)
    {
        using var conn = CreateConnection();

        return await conn.QuerySingleOrDefaultAsync<(string Email, string ContactName)>(
            @"SELECT email, contact_name
          FROM company_contacts
          WHERE company_id = @companyId
            AND contact_type = 'PRIMARY'",
            new { companyId }
        );
    }

    
    public async Task<CompanyLoginDataDto?> GetLoginDataAsync(string email)
    {
        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<CompanyLoginDataDto>(
            @"SELECT 
              c.id            AS CompanyId,
              c.company_name  AS CompanyName,
              c.password_hash AS PasswordHash,
              c.is_sla_signed AS IsSlaSigned,
              c.is_password_changed AS IsPasswordChanged,
          FROM companies c
          INNER JOIN company_contacts cc
              ON cc.company_id = c.id
          WHERE cc.email = @email
            AND cc.contact_type = 'PRIMARY'
            AND c.isapproved = TRUE",
            new { email }
        );
    }

    
    
 public async Task<CompanyDto> GetDetailsAsync(Guid companyId)
{
    using var conn = CreateConnection();

    // 1️⃣ COMPANY
    var company = await conn.QuerySingleAsync<CompanyDto>(
        @"SELECT 
              id,
              company_name              AS CompanyName,
              company_website           AS CompanyWebsite,
              business_type             AS BusinessType,
              company_size              AS CompanySize,
              year_established          AS YearEstablished,
              company_overview          AS CompanyOverview,

              total_projects_executed   AS TotalProjectsExecuted,
              domain_expertise          AS DomainExpertise,

              isapproved                AS IsApproved,
              is_sla_signed             AS IsSlaSigned,
              remark,
              created_at                AS CreatedAt
          FROM companies
          WHERE id = @companyId",
        new { companyId });

    // 2️⃣ CONTACTS
    var contacts = (await conn.QueryAsync<CompanyContact>(
        @"SELECT 
              id,
              company_id        AS CompanyId,
              contact_type      AS ContactType,
              contact_name      AS ContactName,
              role_designation  AS RoleDesignation,
              email             AS Email,
              phone             AS Phone
          FROM company_contacts
          WHERE company_id = @companyId
          ORDER BY contact_type",   
        new { companyId })).ToList();

    // 3️⃣ ADDRESSES
    var addresses = (await conn.QueryAsync<CompanyAddress>(
        @"SELECT 
              id,
              company_id        AS CompanyId,
              address_line1     AS AddressLine1,
              address_line2     AS AddressLine2,
              city              AS City,
              state             AS State,
              postal_code       AS PostalCode,
              country           AS Country
          FROM company_addresses
          WHERE company_id = @companyId",
        new { companyId })).ToList();

    // 4️⃣ CERTIFICATIONS
    var certifications = (await conn.QueryAsync<CompanyCertification>(
        @"SELECT 
              id,
              company_id         AS CompanyId,
              certification_name AS CertificationName
          FROM company_certifications
          WHERE company_id = @companyId",
        new { companyId })).ToList();

    // 5️⃣ ASSIGN
    company.Contacts = contacts;
    company.Addresses = addresses;
    company.Certifications = certifications;

    return company;
}

  
 public async Task<IEnumerable<CompanyDto>> GetAllSuppliersAsync()
{
    using var conn = CreateConnection();

    var sql = @"
    SELECT
        c.id                    AS ""Id"",
        c.company_name          AS ""CompanyName"",
        c.company_website       AS ""CompanyWebsite"",
        c.business_type         AS ""BusinessType"",
        c.company_size          AS ""CompanySize"",
        c.year_established      AS ""YearEstablished"",
        c.company_overview      AS ""CompanyOverview"",
        c.isapproved            AS ""IsApproved"",
        c.is_sla_signed         AS ""IsSlaSigned"",
        c.remark                AS ""Remark"",
        c.total_projects_executed AS ""TotalProjectsExecuted"",
        c.domain_expertise      AS ""DomainExpertise"",
        c.created_at            AS ""CreatedAt"",

        -- Contacts
        json_agg(
            DISTINCT jsonb_build_object(
                'ContactType', cc.contact_type,
                'ContactName', cc.contact_name,
                'RoleDesignation', cc.role_designation,
                'Email', cc.email,
                'Phone', cc.phone
            )
        ) FILTER (WHERE cc.id IS NOT NULL) AS ""Contacts"",

        -- Addresses
        json_agg(
            DISTINCT jsonb_build_object(
                'AddressLine1', a.address_line1,
                'AddressLine2', a.address_line2,
                'City', a.city,
                'State', a.state,
                'PostalCode', a.postal_code,
                'Country', a.country
            )
        ) FILTER (WHERE a.id IS NOT NULL) AS ""Addresses"",

        -- Certifications
        json_agg(
            DISTINCT jsonb_build_object(
                'CertificationName', cert.certification_name
            )
        ) FILTER (WHERE cert.id IS NOT NULL) AS ""Certifications""

    FROM companies c
    LEFT JOIN company_contacts cc ON cc.company_id = c.id
    LEFT JOIN company_addresses a ON a.company_id = c.id
    LEFT JOIN company_certifications cert ON cert.company_id = c.id

    GROUP BY c.id
    ORDER BY c.created_at DESC;
    ";

    return await conn.QueryAsync<CompanyDto>(sql);
}
 
 
 public async Task<IEnumerable<SupplierCapacity>> GetSupplierRejectedAsync(Guid companyId)
{
    using var conn = CreateConnection();

    var sql = @"
        SELECT 
            sc.id                    AS ""Id"",
            sc.companyid             AS ""CompanyId"",
            sc.companyemployeeid     AS ""CompanyEmployeeId"",
            sc.isrefered             AS ""IsRefered"",
            sc.workingsince          AS ""WorkingSince"",
            sc.ctc                   AS ""CTC"",
            sc.jobtitle              AS ""JobTitle"",
            sc.role                  AS ""Role"",
            sc.gender                AS ""Gender"",
            sc.location              AS ""Location"",
            sc.totalexperience       AS ""TotalExperience"",
            sc.technicalskills       AS ""TechnicalSkills"",
            sc.tools                 AS ""Tools"",
            sc.numberofprojects      AS ""NumberOfProjects"",
            sc.status                AS ""Status"",
            sc.approval_stage        AS ""ApprovalStage"",
            sc.employernote          AS ""EmployerNote"",
            sc.remark                AS ""Remark"",
            c.company_name           AS ""CompanyName"",
            cert.id                  AS cert_id,
            cert.certificationname   AS ""CertificationName""
        FROM suppliercapacity sc
        INNER JOIN companies c ON c.id = sc.companyid
        LEFT JOIN suppliercertifications cert 
            ON sc.id = cert.suppliercapacityid
        WHERE sc.companyid = @CompanyId
          AND sc.status = 'Rejected'
          AND sc.approval_stage = 'Supplier';
    ";

    var dict = new Dictionary<Guid, SupplierCapacity>();

    await conn.QueryAsync<SupplierCapacity, SupplierCertification, SupplierCapacity>(
        sql,
        (sc, cert) =>
        {
            if (!dict.TryGetValue(sc.Id, out var capacity))
            {
                capacity = sc;
                capacity.Certifications = new List<SupplierCertification>();
                dict.Add(sc.Id, capacity);
            }

            if (cert != null)
                capacity.Certifications.Add(cert);

            return capacity;
        },
        new { CompanyId = companyId },
        splitOn: "cert_id"
    );

    return dict.Values;
}

 
 
 public async Task<IEnumerable<SupplierCapacity>> GetHrRejectedAsync(Guid companyId)
{
    using var conn = CreateConnection();

    var sql = @"
        SELECT 
            sc.id                    AS ""Id"",
            sc.companyid             AS ""CompanyId"",
            sc.companyemployeeid     AS ""CompanyEmployeeId"",
            sc.isrefered             AS ""IsRefered"",
            sc.workingsince          AS ""WorkingSince"",
            sc.ctc                   AS ""CTC"",
            sc.jobtitle              AS ""JobTitle"",
            sc.role                  AS ""Role"",
            sc.gender                AS ""Gender"",
            sc.location              AS ""Location"",
            sc.totalexperience       AS ""TotalExperience"",
            sc.technicalskills       AS ""TechnicalSkills"",
            sc.tools                 AS ""Tools"",
            sc.numberofprojects      AS ""NumberOfProjects"",
            sc.status                AS ""Status"",
            sc.approval_stage        AS ""ApprovalStage"",
            sc.employernote          AS ""EmployerNote"",
            sc.remark                AS ""Remark"",
            c.company_name           AS ""CompanyName"",
            cert.id                  AS cert_id,
            cert.certificationname   AS ""CertificationName""
        FROM suppliercapacity sc
        INNER JOIN companies c ON c.id = sc.companyid
        LEFT JOIN suppliercertifications cert 
            ON sc.id = cert.suppliercapacityid
        WHERE sc.companyid = @CompanyId
          AND sc.status = 'Rejected'
          AND sc.approval_stage = 'HR';
    ";

    var dict = new Dictionary<Guid, SupplierCapacity>();

    await conn.QueryAsync<SupplierCapacity, SupplierCertification, SupplierCapacity>(
        sql,
        (sc, cert) =>
        {
            if (!dict.TryGetValue(sc.Id, out var capacity))
            {
                capacity = sc;
                capacity.Certifications = new List<SupplierCertification>();
                dict.Add(sc.Id, capacity);
            }

            if (cert != null)
                capacity.Certifications.Add(cert);

            return capacity;
        },
        new { CompanyId = companyId },
        splitOn: "cert_id"
    );

    return dict.Values;
}


}