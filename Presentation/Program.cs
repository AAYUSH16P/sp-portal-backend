using DynamicFormRepo.DynamicFormRepoImplementation;
using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceImplementation;
using DynamicFormService.DynamicFormServiceInterface;
using System.Data;
using Application.Services;
using Dapper;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.DataAccess.Dapper;
using Infrastructure.Email;
using Infrastructure.Templates;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Dapper DB Connection
builder.Services.AddScoped<IDbConnection>(sp =>
    new NpgsqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Repository & Service
builder.Services.AddScoped<ISupplierRepoInterface, SupplierRepoImplementation>();
builder.Services.AddScoped<ISupplierServiceInterface, SupplierServiceImplementation>();
builder.Services.AddScoped<ICompanyApprovalRepo, CompanyApprovalRepo>();
builder.Services.AddScoped<ICompanyApprovalService, CompanyApprovalService>();
builder.Services.AddScoped<ITemplateRenderer, HtmlTemplateRenderer>();


// ✅ REQUIRED: Hangfire storage configuration
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

// ✅ REQUIRED: Hangfire server
builder.Services.AddHangfireServer();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ICalendarRepo, CalendarRepo>();
builder.Services.AddScoped<CalendarAppService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITemplateRenderer, HtmlTemplateRenderer>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICompanyChangeRequestRepository, CompanyChangeRequestRepository>();
builder.Services.AddScoped<ICompanyChangeRequestService, CompanyChangeRequestService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyContact>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyAddress>());
SqlMapper.AddTypeHandler(new JsonListTypeHandler<CompanyCertification>());


var app = builder.Build();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();