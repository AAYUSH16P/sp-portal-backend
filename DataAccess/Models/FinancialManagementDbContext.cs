using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FinancialManagementDataAccess.Models;

public partial class FinancialManagementDbContext : DbContext
{
    public FinancialManagementDbContext()
    {
    }

    public FinancialManagementDbContext(DbContextOptions<FinancialManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectRole> ProjectRoles { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceWorkArrangementOverride> ResourceWorkArrangementOverrides { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<User1> Users1 { get; set; }

    public virtual DbSet<WorkArrangement> WorkArrangements { get; set; }

   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("work_arrangement_type", new[] { "(WFM/Remote - Flexible) UK", "(WFM/Remote - Flexible) Landed", "(On-Site) - UK", "(On-Site) - Landed", "(Remote) - UK", "(Returner remote) - UK" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("projects_pkey");

            entity.ToTable("projects");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClientName).HasColumnName("client_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TeamName).HasColumnName("team_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Projects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("projects_user_id_fkey");
        });

        modelBuilder.Entity<ProjectRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_roles_pkey");

            entity.ToTable("project_roles");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RoleName).HasColumnName("role_name");
            entity.Property(e => e.RoleSpecification).HasColumnName("role_specification");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectRoles)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("project_roles_project_id_fkey");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resources_pkey");

            entity.ToTable("resources");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AcrCommission).HasColumnName("acr_commission");
            entity.Property(e => e.AcrRate).HasColumnName("acr_rate");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.BloodGroup).HasColumnName("blood_group");
            entity.Property(e => e.CirRate).HasColumnName("cir_rate");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ExpectedWorkingDays).HasColumnName("expected_working_days");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PastProjects).HasColumnName("past_projects");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectRoleId).HasColumnName("project_role_id");
            entity.Property(e => e.ResourceType).HasColumnName("resource_type");
            entity.Property(e => e.ResumeUrl).HasColumnName("resume_url");
            entity.Property(e => e.SkillsExpertise).HasColumnName("skills_expertise");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkArrangementId).HasColumnName("work_arrangement_id");

            entity.HasOne(d => d.Project).WithMany(p => p.Resources)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("resources_project_id_fkey");

            entity.HasOne(d => d.ProjectRole).WithMany(p => p.Resources)
                .HasForeignKey(d => d.ProjectRoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("resources_project_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Resources)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("resources_user_id_fkey");

            entity.HasOne(d => d.WorkArrangement).WithMany(p => p.Resources)
                .HasForeignKey(d => d.WorkArrangementId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("resources_work_arrangement_id_fkey");
        });

        modelBuilder.Entity<ResourceWorkArrangementOverride>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resource_work_arrangement_overrides_pkey");

            entity.ToTable("resource_work_arrangement_overrides");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BasicRate).HasColumnName("basic_rate");
            entity.Property(e => e.Bau).HasColumnName("bau");
            entity.Property(e => e.DabApex).HasColumnName("dab_apex");
            entity.Property(e => e.DabBigData).HasColumnName("dab_big_data");
            entity.Property(e => e.DabDelphi).HasColumnName("dab_delphi");
            entity.Property(e => e.DayRate).HasColumnName("day_rate");
            entity.Property(e => e.McbCrm).HasColumnName("mcb_crm");
            entity.Property(e => e.NearDelivery).HasColumnName("near_delivery");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.Saiven).HasColumnName("saiven");
            entity.Property(e => e.SpectrumProfit).HasColumnName("spectrum_profit");
            entity.Property(e => e.TaDa).HasColumnName("ta_da");
            entity.Property(e => e.Tolerance).HasColumnName("tolerance");
            entity.Property(e => e.Ws).HasColumnName("ws");

            entity.HasOne(d => d.Resource).WithMany(p => p.ResourceWorkArrangementOverrides)
                .HasForeignKey(d => d.ResourceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("resource_work_arrangement_overrides_resource_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.User)
                .HasForeignKey<User>(d => d.Id)
                .HasConstraintName("users_id_fkey");
        });

        modelBuilder.Entity<User1>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "auth", tb => tb.HasComment("Auth: Stores user login data within a secure schema."));

            entity.HasIndex(e => e.ConfirmationToken, "confirmation_token_idx")
                .IsUnique()
                .HasFilter("((confirmation_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.EmailChangeTokenCurrent, "email_change_token_current_idx")
                .IsUnique()
                .HasFilter("((email_change_token_current)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.EmailChangeTokenNew, "email_change_token_new_idx")
                .IsUnique()
                .HasFilter("((email_change_token_new)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.ReauthenticationToken, "reauthentication_token_idx")
                .IsUnique()
                .HasFilter("((reauthentication_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.RecoveryToken, "recovery_token_idx")
                .IsUnique()
                .HasFilter("((recovery_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.Email, "users_email_partial_key")
                .IsUnique()
                .HasFilter("(is_sso_user = false)");

            entity.HasIndex(e => e.InstanceId, "users_instance_id_idx");

            entity.HasIndex(e => e.IsAnonymous, "users_is_anonymous_idx");

            entity.HasIndex(e => e.Phone, "users_phone_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Aud)
                .HasMaxLength(255)
                .HasColumnName("aud");
            entity.Property(e => e.BannedUntil).HasColumnName("banned_until");
            entity.Property(e => e.ConfirmationSentAt).HasColumnName("confirmation_sent_at");
            entity.Property(e => e.ConfirmationToken)
                .HasMaxLength(255)
                .HasColumnName("confirmation_token");
            entity.Property(e => e.ConfirmedAt)
                .HasComputedColumnSql("LEAST(email_confirmed_at, phone_confirmed_at)", true)
                .HasColumnName("confirmed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailChange)
                .HasMaxLength(255)
                .HasColumnName("email_change");
            entity.Property(e => e.EmailChangeConfirmStatus)
                .HasDefaultValue((short)0)
                .HasColumnName("email_change_confirm_status");
            entity.Property(e => e.EmailChangeSentAt).HasColumnName("email_change_sent_at");
            entity.Property(e => e.EmailChangeTokenCurrent)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("email_change_token_current");
            entity.Property(e => e.EmailChangeTokenNew)
                .HasMaxLength(255)
                .HasColumnName("email_change_token_new");
            entity.Property(e => e.EmailConfirmedAt).HasColumnName("email_confirmed_at");
            entity.Property(e => e.EncryptedPassword)
                .HasMaxLength(255)
                .HasColumnName("encrypted_password");
            entity.Property(e => e.InstanceId).HasColumnName("instance_id");
            entity.Property(e => e.InvitedAt).HasColumnName("invited_at");
            entity.Property(e => e.IsAnonymous)
                .HasDefaultValue(false)
                .HasColumnName("is_anonymous");
            entity.Property(e => e.IsSsoUser)
                .HasDefaultValue(false)
                .HasComment("Auth: Set this column to true when the account comes from SSO. These accounts can have duplicate emails.")
                .HasColumnName("is_sso_user");
            entity.Property(e => e.IsSuperAdmin).HasColumnName("is_super_admin");
            entity.Property(e => e.LastSignInAt).HasColumnName("last_sign_in_at");
            entity.Property(e => e.Phone)
                .HasDefaultValueSql("NULL::character varying")
                .HasColumnName("phone");
            entity.Property(e => e.PhoneChange)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("phone_change");
            entity.Property(e => e.PhoneChangeSentAt).HasColumnName("phone_change_sent_at");
            entity.Property(e => e.PhoneChangeToken)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("phone_change_token");
            entity.Property(e => e.PhoneConfirmedAt).HasColumnName("phone_confirmed_at");
            entity.Property(e => e.RawAppMetaData)
                .HasColumnType("jsonb")
                .HasColumnName("raw_app_meta_data");
            entity.Property(e => e.RawUserMetaData)
                .HasColumnType("jsonb")
                .HasColumnName("raw_user_meta_data");
            entity.Property(e => e.ReauthenticationSentAt).HasColumnName("reauthentication_sent_at");
            entity.Property(e => e.ReauthenticationToken)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("reauthentication_token");
            entity.Property(e => e.RecoverySentAt).HasColumnName("recovery_sent_at");
            entity.Property(e => e.RecoveryToken)
                .HasMaxLength(255)
                .HasColumnName("recovery_token");
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<WorkArrangement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("work_arrangements_pkey");

            entity.ToTable("work_arrangements");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BasicRate).HasColumnName("basic_rate");
            entity.Property(e => e.Bau).HasColumnName("bau");
            entity.Property(e => e.DabApex).HasColumnName("dab_apex");
            entity.Property(e => e.DabBigData).HasColumnName("dab_big_data");
            entity.Property(e => e.DabDelphi).HasColumnName("dab_delphi");
            entity.Property(e => e.DayRate).HasColumnName("day_rate");
            entity.Property(e => e.McbCrm).HasColumnName("mcb_crm");
            entity.Property(e => e.NearDelivery).HasColumnName("near_delivery");
            entity.Property(e => e.ProjectRoleId).HasColumnName("project_role_id");
            entity.Property(e => e.Saiven).HasColumnName("saiven");
            entity.Property(e => e.SpectrumProfit).HasColumnName("spectrum_profit");
            entity.Property(e => e.TaDa).HasColumnName("ta_da");
            entity.Property(e => e.Tolerance).HasColumnName("tolerance");
            entity.Property(e => e.Ws).HasColumnName("ws");

            entity.HasOne(d => d.ProjectRole).WithMany(p => p.WorkArrangements)
                .HasForeignKey(d => d.ProjectRoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("work_arrangements_project_role_id_fkey");
        });
        modelBuilder.HasSequence<int>("seq_schema_version", "graphql").IsCyclic();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
