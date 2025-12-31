using System.Data;
using Application.Services;
using DynamicFormRepo.DynamicFormRepoImplementation;
using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceImplementation;
using DynamicFormService.DynamicFormServiceInterface;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

// 🚀 Railway PORT binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddControllers();

// 🔐 PostgreSQL connection
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

connectionString += ";SSL Mode=Require;Trust Server Certificate=true";

// Dapper
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(connectionString)
);

// Hangfire
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(connectionString);
});
builder.Services.AddHangfireServer();

// Services & repos (unchanged)
builder.Services.AddScoped<ISupplierRepoInterface, SupplierRepoImplementation>();
builder.Services.AddScoped<ISupplierServiceInterface, SupplierServiceImplementation>();
builder.Services.AddScoped<ICompanyApprovalRepo, CompanyApprovalRepo>();
builder.Services.AddScoped<ICompanyApprovalService, CompanyApprovalService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger (enable for Railway)
app.UseSwagger();
app.UseSwaggerUI();

// Hangfire dashboard
app.UseHangfireDashboard("/hangfire");

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
