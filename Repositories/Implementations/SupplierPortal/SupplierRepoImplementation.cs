using DynamicFormRepo.DynamicFormRepoInterface;
using System.Data;
using Shared;
using Dapper;
using FinancialManagementDataAccess.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Shared.Enum;

namespace DynamicFormRepo.DynamicFormRepoImplementation
{
    public class SupplierRepoImplementation : ISupplierRepoInterface
    {
        private readonly string _connectionString;
        private readonly ILogger<SupplierRepoImplementation> _logger;

        public SupplierRepoImplementation(IDbConnection db,ILogger<SupplierRepoImplementation> logger)
        {
            _connectionString = db.ConnectionString;
            _logger = logger;

        }

        private IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
        
    public async Task<Guid> SubmitCompanyAsync(CompanyRegistrationRequestDto request)
{
    _logger.LogInformation("SubmitCompanyAsync started");

    // 🔍 CRITICAL LOG: incoming value
    _logger.LogInformation(
        "Incoming Total Projects Executed (ProjectExecuted) = {ProjectExecuted}",
        request.ProjectExecuted
    );

    using var conn = CreateConnection();
    await ((NpgsqlConnection)conn).OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // 🔍 Log before insert
        var totalProjectsExecuted = request.ProjectExecuted;
        _logger.LogInformation(
            "Value being sent to DB for total_projects_executed = {TotalProjectsExecuted}",
            totalProjectsExecuted
        );

        // 1️⃣ Insert Company
        var companyId = await conn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO companies
            (
                company_name,
                company_website,
                business_type,
                company_size,
                year_established,
                company_overview,
                total_projects_executed,
                domain_expertise,
                isapproved,
                is_sla_signed
            )
            VALUES
            (
                @CompanyName,
                @CompanyWebsite,
                @BusinessType,
                @CompanySize,
                @YearEstablished,
                @CompanyOverview,
                @TotalProjectsExecuted,
                @DomainExpertise,
                FALSE,
                FALSE
            )
            RETURNING id;
        ", new
        {
            request.CompanyName,
            request.CompanyWebsite,
            request.BusinessType,
            request.CompanySize,
            request.YearEstablished,
            request.CompanyOverview,
            TotalProjectsExecuted = totalProjectsExecuted, // 👈 explicit variable
            request.DomainExpertise
        }, transaction);

        _logger.LogInformation(
            "Company inserted successfully. CompanyId = {CompanyId}",
            companyId
        );

        // 🔍 OPTIONAL: verify immediately from DB
        var dbValue = await conn.ExecuteScalarAsync<int?>(
            "SELECT total_projects_executed FROM companies WHERE id = @CompanyId",
            new { CompanyId = companyId },
            transaction
        );

        _logger.LogInformation(
            "DB value after insert for total_projects_executed = {DbValue}",
            dbValue
        );

        // 2️⃣ Insert Address
        await conn.ExecuteAsync(@"
            INSERT INTO company_addresses
            (company_id, address_line1, address_line2, city, state, postal_code, country)
            VALUES
            (@CompanyId, @AddressLine1, @AddressLine2, @City, @State, @PostalCode, @Country);
        ", new
        {
            CompanyId = companyId,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country
        }, transaction);

        // 3️⃣ Insert PRIMARY Contact
        await conn.ExecuteAsync(@"
            INSERT INTO company_contacts
            (company_id, contact_type, contact_name, role_designation, email, phone)
            VALUES
            (@CompanyId, 'PRIMARY', @Name, @Role, @Email, @Phone);
        ", new
        {
            CompanyId = companyId,
            Name = request.PrimaryContactName,
            Role = request.PrimaryContactRole,
            Email = request.PrimaryContactEmail,
            Phone = request.PrimaryContactPhone
        }, transaction);

        // 4️⃣ Insert SECONDARY Contact
        if (!string.IsNullOrWhiteSpace(request.SecondaryContactName))
        {
            await conn.ExecuteAsync(@"
                INSERT INTO company_contacts
                (company_id, contact_type, contact_name, role_designation, email, phone)
                VALUES
                (@CompanyId, 'SECONDARY', @Name, @Role, @Email, @Phone);
            ", new
            {
                CompanyId = companyId,
                Name = request.SecondaryContactName,
                Role = request.SecondaryContactRole,
                Email = request.SecondaryContactEmail,
                Phone = request.SecondaryContactPhone
            }, transaction);
        }

        // 5️⃣ Insert Certifications
        foreach (var cert in request.Certifications ?? Enumerable.Empty<string>())
        {
            await conn.ExecuteAsync(@"
                INSERT INTO company_certifications
                (company_id, certification_name)
                VALUES (@CompanyId, @Certification);
            ", new
            {
                CompanyId = companyId,
                Certification = cert
            }, transaction);
        }

        transaction.Commit();
        _logger.LogInformation("SubmitCompanyAsync completed successfully");

        return companyId;
    }
    catch (Exception ex)
    {
        transaction.Rollback();

        _logger.LogError(
            ex,
            "SubmitCompanyAsync failed. ProjectExecuted = {ProjectExecuted}",
            request.ProjectExecuted
        );

        throw;
    }
}

        public async Task<long> CreateUploadAsync(
            Guid companyId,
            int uploadedBy,
            string fileName)
        {
            using var conn = CreateConnection();

            return await conn.ExecuteScalarAsync<long>(@"
        INSERT INTO bulk_upload_history
        (
            company_id,
            uploaded_by,
            file_name,
            status
        )
        VALUES
        (
            @CompanyId,
            @UploadedBy,
            @FileName,
            'Processing'
        )
        RETURNING upload_id;
    ",
                new
                {
                    CompanyId = companyId,
                    UploadedBy = uploadedBy,
                    FileName = fileName
                });
        }



        public async Task InsertErrorAsync(long uploadId, int rowNumber, string reason)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO bulk_upload_errors (upload_id, row_number, error_reason)
                VALUES (@UploadId, @RowNumber, @Reason);
            ", new { UploadId = uploadId, RowNumber = rowNumber, Reason = reason });
        }

        public async Task UpdateUploadAsync(long uploadId, int total, int success, int failure, string status)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE bulk_upload_history
                SET total_rows=@Total, success_count=@Success, failure_count=@Failure, status=@Status
                WHERE upload_id=@UploadId;
            ", new { UploadId = uploadId, Total = total, Success = success, Failure = failure, Status = status });
        }

        public async Task SaveFileAsync(long uploadId, byte[] content, long size)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO upload_files (upload_id, file_content, file_size)
                VALUES (@UploadId, @Content, @Size);
            ", new { UploadId = uploadId, Content = content, Size = size });
        }

        public async Task<byte[]> GetFileAsync(long uploadId)
        {
            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<byte[]>(@"
                SELECT file_content FROM upload_files WHERE upload_id=@UploadId;
            ", new { UploadId = uploadId });
        }

        public async Task DeleteFileAsync(long uploadId)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("DELETE FROM upload_files WHERE upload_id=@UploadId", new { UploadId = uploadId });
        }

     public async Task InsertSupplierResourcesBatchAsync(List<SupplierResourceDto> batch)
{
    using var conn = CreateConnection();
    await ((NpgsqlConnection)conn).OpenAsync();
    using var tx = conn.BeginTransaction();

    try
    {
        // =========================================
        // UPDATED: Added Status & Approval_Stage
        // =========================================
        var sql = @"
           INSERT INTO suppliercapacity
            (
                companyemployeeid,
                companyid,
                isrefered,
                workingsince,
                ctc,
                jobtitle,
                role,
                gender,
                location,
                totalexperience,
                technicalskills,
                tools,
                numberofprojects,
                employernote,
                status,
                approval_stage
            )
            SELECT
                u.companyemployeeid,
                u.companyid,
                u.isrefered,
                u.workingsince,
                u.ctc,
                u.jobtitle,
                u.role,
                u.gender,
                u.location,
                u.totalexperience,
                u.technicalskills,
                u.tools,
                u.numberofprojects,
                u.employernote,
                u.status::supplier_status,           -- 🔥 CAST HERE
                u.approval_stage                      -- VARCHAR column
            FROM UNNEST
            (
                @CompanyEmployeeId,
                @CompanyId,
                @IsRefered,
                @WorkingSince,
                @CTC,
                @JobTitle,
                @Role,
                @Gender,
                @Location,
                @TotalExperience,
                @TechnicalSkills,
                @Tools,
                @NumberOfProjects,
                @EmployerNote,
                @Status,
                @ApprovalStage
            )
            AS u
            (
                companyemployeeid,
                companyid,
                isrefered,
                workingsince,
                ctc,
                jobtitle,
                role,
                gender,
                location,
                totalexperience,
                technicalskills,
                tools,
                numberofprojects,
                employernote,
                status,
                approval_stage
            )
            RETURNING id";

        // =========================================
        // UPDATED: Passing Status & ApprovalStage
        // =========================================
        var ids = (await conn.QueryAsync<Guid>(
            sql,
            new
            {
                CompanyEmployeeId = batch.Select(x => x.CompanyEmployeeId).ToArray(),
                CompanyId = batch.Select(x => x.CompanyId).ToArray(),
                IsRefered = batch.Select(x => x.IsRefered).ToArray(),
                WorkingSince = batch.Select(x => x.WorkingSince).ToArray(),
                CTC = batch.Select(x => x.CTC).ToArray(),
                JobTitle = batch.Select(x => x.JobTitle).ToArray(),
                Role = batch.Select(x => x.Role).ToArray(),
                Gender = batch.Select(x => x.Gender).ToArray(),
                Location = batch.Select(x => x.Location).ToArray(),
                TotalExperience = batch.Select(x => x.TotalExperience).ToArray(),
                TechnicalSkills = batch.Select(x => x.TechnicalSkills).ToArray(),
                Tools = batch.Select(x => x.Tools).ToArray(),
                NumberOfProjects = batch.Select(x => x.NumberOfProjects).ToArray(),
                EmployerNote = batch.Select(x => x.EmployerNote).ToArray(),

                // 🔥 NEW FIELDS
                Status = batch.Select(x => x.Status.ToString()).ToArray(),
                ApprovalStage = batch.Select(x => x.ApprovalStage.ToString()).ToArray()
            },
            tx
        )).ToList();

        // =========================================
        // Certifications insert (UNCHANGED)
        // =========================================
        var certSql = @"
            INSERT INTO suppliercertifications
            (suppliercapacityid, certificationname)
            VALUES (@Id, @Cert);
        ";

        for (int i = 0; i < batch.Count; i++)
        {
            if (batch[i].Certifications == null || batch[i].Certifications.Count == 0)
                continue;

            foreach (var cert in batch[i].Certifications)
            {
                await conn.ExecuteAsync(
                    certSql,
                    new
                    {
                        Id = ids[i],
                        Cert = cert
                    },
                    tx
                );
            }
        }

        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}

      
        public async Task MarkSlaSignedAsync(Guid companyId)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(@"
            UPDATE companies
            SET is_sla_signed = TRUE,
                updated_at = NOW()
            WHERE id = @CompanyId;
        ", new { CompanyId = companyId });
        }
        
        
        
        
        public async Task SetPasswordAsync(Guid companyId, string passwordHash)
            {
                using var conn = CreateConnection();

                await conn.ExecuteAsync(@"
                                        UPDATE companies
                                        SET password_hash = @PasswordHash,
                                            updated_at = NOW(),
                                            is_password_changed = true
                                        WHERE id = @CompanyId;
                                    ", new
                {
                    CompanyId = companyId,
                    PasswordHash = passwordHash
                });
            }

       public async Task<IEnumerable<SupplierCapacity>> GetByStageAsync(
            Guid companyId,
            ApprovalStage stage,
            SupplierStatus? status)
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
                        sc.createdat             AS ""CreatedAt"",

                        c.company_name           AS ""CompanyName"",

                        cert.id                  AS cert_id,
                        cert.certificationname   AS ""CertificationName""
                    FROM suppliercapacity sc
                    INNER JOIN companies c
                        ON c.id = sc.companyid
                    LEFT JOIN suppliercertifications cert
                        ON sc.id = cert.suppliercapacityid
                    WHERE sc.companyid = @CompanyId
                     AND sc.approval_stage IN ('Supplier', 'Completed')
                      AND (
                            @Status IS NULL
                            OR sc.status = @Status::supplier_status
                          )
                    ORDER BY sc.createdat DESC;
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
                    new
                    {
                        CompanyId = companyId,
                        Stage = stage.ToString(),
                        Status = status?.ToString()
                    },
                    splitOn: "cert_id"
                );

                return dict.Values;
            }

        
        
        
        public async Task<IEnumerable<SupplierCapacity>> GetAllDataByStageAsync(
    ApprovalStage stage,
    SupplierStatus? status)
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
            sc.createdat             AS ""CreatedAt"",

            c.company_name           AS ""CompanyName"",

            cert.id                  AS cert_id,
            cert.certificationname   AS ""CertificationName""
        FROM suppliercapacity sc
        INNER JOIN companies c
            ON c.id = sc.companyid
        LEFT JOIN suppliercertifications cert
            ON sc.id = cert.suppliercapacityid
        WHERE sc.approval_stage = @Stage
          AND (
                @Status IS NULL
                OR sc.status = @Status::supplier_status
              )
        ORDER BY sc.createdat DESC;
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
        new
        {
            Stage = stage.ToString(),
            Status = status?.ToString()
        },
        splitOn: "cert_id"
    );

    return dict.Values;
}

        
        

        public async Task<SupplierCapacity> GetByIdAsync(Guid id)
        {
            using var conn = CreateConnection();

            var sql = @"SELECT * FROM suppliercapacity WHERE id = @Id";
            return await conn.QuerySingleAsync<SupplierCapacity>(sql, new { Id = id });
        }
        public async Task UpdateAsync(SupplierCapacity capacity)
        {
            using var conn = CreateConnection();

            var sql = @"
        UPDATE suppliercapacity SET
            status = @Status::supplier_status,
            approval_stage = @ApprovalStage,
            remark = @Remark
        WHERE id = @Id;
    ";

            await conn.ExecuteAsync(sql, new
            {
                Status = capacity.Status.ToString(),
                ApprovalStage = capacity.ApprovalStage.ToString(),
                capacity.Remark,
                capacity.Id
            });
        }
        
        
        
        
        
        
    public async Task UpdateAsyncReferEmployee(SupplierCapacity entity)
{
    using var conn = CreateConnection();
    conn.Open();

    using var transaction = conn.BeginTransaction();

    try
    {
        // 1️⃣ Update SupplierCapacity
        var updateCapacitySql = @"
            UPDATE SupplierCapacity
            SET
                CompanyEmployeeId = @CompanyEmployeeId,
                WorkingSince = @WorkingSince,
                CTC = @CTC,
                JobTitle = @JobTitle,
                Role = @Role,
                Gender = @Gender,
                Location = @Location,
                TotalExperience = @TotalExperience,
                TechnicalSkills = @TechnicalSkills,
                Tools = @Tools,
                NumberOfProjects = @NumberOfProjects,
                EmployerNote = @EmployerNote
            WHERE Id = @Id;
        ";

        await conn.ExecuteAsync(
            updateCapacitySql,
            new
            {
                entity.Id,
                entity.CompanyEmployeeId,

                // ✅ ALWAYS VALID DateTime
                WorkingSince = entity.WorkingSince.ToDateTime(TimeOnly.MinValue),

                entity.CTC,
                entity.JobTitle,
                entity.Role,
                entity.Gender,
                entity.Location,
                entity.TotalExperience,
                entity.TechnicalSkills,
                entity.Tools,
                entity.NumberOfProjects,
                entity.EmployerNote
            },
            transaction
        );

        // 2️⃣ Delete old certifications
        await conn.ExecuteAsync(
            @"DELETE FROM SupplierCertifications WHERE SupplierCapacityId = @Id;",
            new { entity.Id },
            transaction
        );

        // 3️⃣ Insert new certifications
        if (entity.Certifications.Any())
        {
            await conn.ExecuteAsync(
                @"INSERT INTO SupplierCertifications
                  (Id, SupplierCapacityId, CertificationName)
                  VALUES (@Id, @SupplierCapacityId, @CertificationName);",
                entity.Certifications.Select(c => new
                {
                    c.Id,
                    SupplierCapacityId = entity.Id,
                    c.CertificationName
                }),
                transaction
            );
        }

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

    
public async Task<string?> GetPasswordHashAsync(Guid companyId)
{
    using var conn = CreateConnection();

    return await conn.QuerySingleOrDefaultAsync<string>(@"
        SELECT password_hash
        FROM companies
        WHERE id = @CompanyId
    ", new { CompanyId = companyId });
}



   public async Task UpdateCompanyAsync(Guid companyId, UpdateCompanyRequestDto dto)
{
    using var conn = CreateConnection();
    await ((NpgsqlConnection)conn).OpenAsync();
    using var tx = conn.BeginTransaction();

    try
    {
        /* 1️⃣ Update Company */
        await conn.ExecuteAsync(@"
            UPDATE companies
            SET
                company_name = @CompanyName,
                company_website = @CompanyWebsite,
                business_type = @BusinessType,
                company_size = @CompanySize,
                year_established = @YearEstablished,
                company_overview = @CompanyOverview,
                total_projects_executed = @TotalProjectsExecuted,
                domain_expertise = @DomainExpertise,
                updated_at = NOW()
            WHERE id = @CompanyId;
        ", new
        {
            CompanyId = companyId,
            dto.CompanyName,
            dto.CompanyWebsite,
            dto.BusinessType,
            dto.CompanySize,
            dto.YearEstablished,
            dto.CompanyOverview,
            dto.TotalProjectsExecuted,
            dto.DomainExpertise
        }, tx);

        /* 2️⃣ Update Address */
        await conn.ExecuteAsync(@"
            UPDATE company_addresses
            SET
                address_line1 = @AddressLine1,
                address_line2 = @AddressLine2,
                city = @City,
                state = @State,
                postal_code = @PostalCode,
                country = @Country
            WHERE company_id = @CompanyId;
        ", new
        {
            CompanyId = companyId,
            dto.Address.AddressLine1,
            dto.Address.AddressLine2,
            dto.Address.City,
            dto.Address.State,
            dto.Address.PostalCode,
            dto.Address.Country
        }, tx);

        /* 3️⃣ Update PRIMARY contact */
        await conn.ExecuteAsync(@"
            UPDATE company_contacts
            SET
                contact_name = @ContactName,
                role_designation = @Role,
                email = @Email,
                phone = @Phone
            WHERE company_id = @CompanyId AND contact_type = 'PRIMARY';
        ", new
        {
            CompanyId = companyId,
            ContactName = dto.PrimaryContact.ContactName,
            Role = dto.PrimaryContact.RoleDesignation,
            Email = dto.PrimaryContact.Email,
            Phone = dto.PrimaryContact.Phone
        }, tx);

        /* 4️⃣ Replace SECONDARY contact */
        await conn.ExecuteAsync(
            @"DELETE FROM company_contacts 
              WHERE company_id = @CompanyId AND contact_type = 'SECONDARY';",
            new { CompanyId = companyId }, tx);

        if (dto.SecondaryContact != null)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO company_contacts
                (company_id, contact_type, contact_name, role_designation, email, phone)
                VALUES
                (@CompanyId, 'SECONDARY', @Name, @Role, @Email, @Phone);
            ", new
            {
                CompanyId = companyId,
                Name = dto.SecondaryContact.ContactName,
                Role = dto.SecondaryContact.RoleDesignation,
                Email = dto.SecondaryContact.Email,
                Phone = dto.SecondaryContact.Phone
            }, tx);
        }

        /* 5️⃣ Replace Certifications */
        await conn.ExecuteAsync(
            "DELETE FROM company_certifications WHERE company_id = @CompanyId;",
            new { CompanyId = companyId }, tx);

        foreach (var cert in dto.Certifications)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO company_certifications
                (company_id, certification_name)
                VALUES (@CompanyId, @Certification);
            ", new
            {
                CompanyId = companyId,
                Certification = cert
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
   
   
   


  public async Task<IEnumerable<SupplierResourceDto>> GetEligibleSuppliersAsync()
{
    const string sql = @"
    SELECT
        sc.id,
        sc.companyemployeeid,
        sc.companyid,
        c.company_name        AS companyname,   -- ✅ added
        sc.isrefered,
        sc.workingsince,
        sc.ctc,
        sc.jobtitle,
        sc.role,
        sc.gender,
        sc.location,
        sc.totalexperience,
        sc.technicalskills,
        sc.tools,
        sc.numberofprojects,
        sc.status,
        sc.approval_stage,
        sc.employernote,
        COALESCE(
            json_agg(
                jsonb_build_object(
                    'CertificationName', cert.certificationname
                )
            ) FILTER (WHERE cert.id IS NOT NULL),
            '[]'
        ) AS certifications
    FROM public.suppliercapacity sc
    LEFT JOIN public.companies c
        ON c.id = sc.companyid                 -- ✅ join companies
    LEFT JOIN public.suppliercertifications cert
        ON cert.suppliercapacityid = sc.id
    WHERE sc.approval_stage = 'Supplier'
      AND AGE(CURRENT_DATE, sc.workingsince) >= INTERVAL '1 year'
    GROUP BY
        sc.id,
        c.company_name;                        -- ✅ required
    ";

    using var conn = CreateConnection();

    IEnumerable<dynamic> rows = await conn.QueryAsync(sql);

    List<SupplierResourceDto> result = new();

    foreach (dynamic row in rows)
    {
        SupplierStatus? status = null;
        ApprovalStage? stage = null;

        SupplierStatus parsedStatus = default;
        ApprovalStage parsedStage = default;

        if (row.status != null &&
            Enum.TryParse(row.status.ToString(), true, out parsedStatus))
        {
            status = parsedStatus;
        }

        if (row.approval_stage != null &&
            Enum.TryParse(row.approval_stage.ToString(), true, out parsedStage))
        {
            stage = parsedStage;
        }

        string certificationsJson = row.certifications != null
            ? row.certifications.ToString()
            : "[]";

        List<string> certifications =
            Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<CertificationTemp>>(certificationsJson)?
                .Select(c => c.CertificationName)
                .ToList()
            ?? new List<string>();

        SupplierResourceDto dto = new SupplierResourceDto
        {
            Id = (Guid)row.id,
            CompanyEmployeeId = (string)row.companyemployeeid,
            CompanyId = (Guid)row.companyid,
            CompanyName = row.companyname as string,   // ✅ mapped
            IsRefered = (bool)row.isrefered,

            WorkingSince = (DateOnly)row.workingsince,

            CTC = (decimal)row.ctc,
            JobTitle = (string)row.jobtitle,
            Role = (string)row.role,
            Gender = (string)row.gender,
            Location = (string)row.location,
            TotalExperience = (decimal)row.totalexperience,
            TechnicalSkills = (string)row.technicalskills,
            Tools = (string)row.tools,
            NumberOfProjects = (int)row.numberofprojects,
            EmployerNote = row.employernote as string,

            Status = status,
            ApprovalStage = stage,
            Certifications = certifications
        };

        result.Add(dto);
    }

    return result;
}

private class CertificationTemp
{
    public string CertificationName { get; set; }
}

   
    }
}
