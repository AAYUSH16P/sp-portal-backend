using System.Data;
using Dapper;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;

using DynamicFormRepo.DynamicFormRepoImplementation;
using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceImplementation;
using DynamicFormService.DynamicFormServiceInterface;

using Application.Services;
using Infrastructure.DataAccess.Dapper;
using Infrastructure.Email;
using Infrastructure.Templates;
using Shared;

var builder = WebApplication.CreateBuilder(args);

//
// =================================================
// 🚀 Railway PORT binding (MANDATORY)
// =================================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

//
// =================================================
// 🔐 PostgreSQL Connection (Railway + Local SAFE)
// =================================================
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// 🔎 TEMP DEBUG (safe to keep)
Console.WriteLine($"DATABASE_URL visible: {Environment.GetEnvironmentVariable("DATABASE_URL") != null}");
Console.WriteLine($"DefaultConnection visible: {builder.Configuration.GetConnectionString("DefaultConnection") != null}");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("No database connection string found (DATABASE_URL or DefaultConnection)");
}

// Railway requires SSL
connectionString += ";SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";

//
// =================================================
// 📦 Controllers
// =================================================
builder.Services.AddControllers();

//
// =================================================
// 🗄️ Dapper DB Connection
// =================================================
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(connectionString)
);

//
// =================================================
// 🧠 Repositories & Services
// =================================================
builder.Services.AddScoped<ISupplierRepoInterface, SupplierRepoImplementation>();
builder.Services.AddScoped<ISupplierServiceInterface, SupplierServiceImplementation>();

builder.Services.AddScoped<ICompanyApprovalRepo, CompanyApprovalRepo>();
builder.Services.AddScoped<ICompanyApprovalService, CompanyApprovalService>();

builder.Services.AddScoped<ICalendarRepo, CalendarRepo>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<CalendarAppService>();

builder.Services.AddScoped<ICompanyChangeRequestRepository, CompanyChangeRequestRepository>();
builder.Services.AddScoped<ICompanyChangeRequestService, CompanyChangeRequestService>();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<ITemplateRenderer, HtmlTemplateRenderer>();

//
// =================================================
// 🔥 Hangfire (FIXED – NO SCHEMA LOOP)
// =================================================
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        PrepareSchemaIfNecessary = false
    });
});

builder.Services.AddHangfireServer();

//
// =================================================
// 🌍 CORS
// =================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

//
// =================================================
// 📘 Swagger
// =================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//
// =================================================
// 🧩 Dapper Type Handlers
// =================================================
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyContact>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyAddress>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyCertification>());

//
// =================================================
// 🚀 Build App
// =================================================
var app = builder.Build();

//
// =================================================
// 📘 Swagger (ENABLED IN PRODUCTION)
// =================================================
app.UseSwagger();
app.UseSwaggerUI();

//
// =================================================
// 🧭 Hangfire Dashboard
// =================================================
app.UseHangfireDashboard("/hangfire");

//
// =================================================
// 🌍 Middleware
// =================================================
app.UseCors("AllowAll");

// ❌ DO NOT USE HTTPS REDIRECTION ON RAILWAY
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

//
// =================================================
// ▶️ Run App
// =================================================
app.Run();
