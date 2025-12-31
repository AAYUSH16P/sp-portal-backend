using System.Data;
using System.Net;
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
// ================================
// 🚀 Railway PORT binding (MANDATORY)
// ================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

//
// ================================
// 🔐 PostgreSQL Connection (Railway FIXED)
// ================================
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new Exception("DATABASE_URL environment variable is missing");
}

var connectionString = BuildNpgsqlConnectionString(databaseUrl);

Console.WriteLine("PostgreSQL connection configured successfully");

//
// ================================
// 📦 Controllers
// ================================
builder.Services.AddControllers();

//
// ================================
// 🗄️ Dapper DB Connection
// ================================
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(connectionString)
);

//
// ================================
// 🧠 Repositories & Services
// ================================
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
// ================================
// 🔥 Hangfire (NO SCHEMA LOOP)
// ================================
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        PrepareSchemaIfNecessary = false // ✅ CRITICAL
    });
});

builder.Services.AddHangfireServer();

//
// ================================
// 🌍 CORS
// ================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

//
// ================================
// 📘 Swagger
// ================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//
// ================================
// 🧩 Dapper JSON Type Handlers
// ================================
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyContact>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyAddress>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyCertification>());

//
// ================================
// 🚀 Build App
// ================================
var app = builder.Build();

//
// ================================
// 📘 Swagger (PRODUCTION ENABLED)
// ================================
app.UseSwagger();
app.UseSwaggerUI();

//
// ================================
// 🧭 Hangfire Dashboard
// ================================
app.UseHangfireDashboard("/hangfire");

//
// ================================
// 🌍 Middleware
// ================================
app.UseCors("AllowAll");

// ❌ DO NOT enable HTTPS redirection on Railway
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

//
// ================================
// ▶️ Run App
// ================================
app.Run();


//
// ================================
// 🔧 Helper: Convert DATABASE_URL → Npgsql
// ================================
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    return $"Host={uri.Host};" +
           $"Port={uri.Port};" +
           $"Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={WebUtility.UrlDecode(userInfo[0])};" +
           $"Password={WebUtility.UrlDecode(userInfo[1])};" +
           $"SSL Mode=Require;" +
           $"Trust Server Certificate=true";
}
