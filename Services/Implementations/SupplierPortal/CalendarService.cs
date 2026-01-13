using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Shared;
using Dapper;
using System.Data;



public class CalendarService : ICalendarService
{
    private readonly GraphServiceClient _client;
    private readonly ICalendarRepo _repo;
    private readonly IEmailService _emailService;
    private readonly ILogger<CalendarService> _logger;
    private readonly IDbConnection _dbConnection;

    
    private static readonly TimeZoneInfo IstZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "India Standard Time"
                : "Asia/Kolkata"
        );


    public CalendarService(IConfiguration config, ICalendarRepo repo, IEmailService emailService, ILogger<CalendarService> logger,IDbConnection dbConnection)
    {
        _client = GraphClientFactory.Create(
            config["AzureAd:TenantId"]!,
            config["AzureAd:ClientId"]!,
            config["AzureAd:ClientSecret"]!
        );
        _repo = repo;
        _emailService = emailService;
        _logger = logger;
        _dbConnection = dbConnection;
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(
        string hostEmail,
        DateTime startUtc,
        DateTime endUtc)
    {
        var result = await _client.Users[hostEmail]
            .CalendarView
            .GetAsync(config =>
            {
                config.QueryParameters.StartDateTime = startUtc.ToString("o");
                config.QueryParameters.EndDateTime = endUtc.ToString("o");

                config.Headers.Add(
                    "Prefer",
                    "outlook.timezone=\"UTC\""
                );
            });

        return result?.Value?
                   .Select(e => new CalendarEventDto
                   {
                       Subject = e.Subject ?? "",
                       StartUtc = DateTime.Parse(e.Start!.DateTime!),
                       EndUtc = DateTime.Parse(e.End!.DateTime!),
                       JoinUrl = e.OnlineMeeting?.JoinUrl
                   })
                   .ToList()
               ?? new List<CalendarEventDto>();
    }


 public async Task<ScheduleMeetingResultDto> ScheduleMeetingAsync(
    string hostEmail,
    ScheduleMeetingDto dto)
{
    _logger.LogInformation(
        "ScheduleMeetingAsync started. HostEmail: {HostEmail}, CompanyId: {CompanyId}",
        hostEmail,
        dto.CompanyId);

    // Safety check
    if (dto.AttendeeEmails == null || !dto.AttendeeEmails.Any())
    {
        _logger.LogWarning("No attendee emails provided. Aborting meeting scheduling.");
        throw new ArgumentException("At least one attendee email is required");
    }

    _logger.LogInformation(
        "Attendee count: {AttendeeCount}",
        dto.AttendeeEmails.Count);

    // 1️⃣ Build attendees
    var attendees = dto.AttendeeEmails.Select(email =>
        new Attendee
        {
            EmailAddress = new EmailAddress { Address = email },
            Type = AttendeeType.Required
        }).ToList();

    _logger.LogInformation("Attendees mapped successfully.");

    // 2️⃣ Create Teams meeting
    var ev = new Event
    {
        Subject = dto.Subject,
        Start = new DateTimeTimeZone
        {
            DateTime = dto.StartUtc.ToString("o"),
            TimeZone = "UTC"
        },
        End = new DateTimeTimeZone
        {
            DateTime = dto.EndUtc.ToString("o"),
            TimeZone = "UTC"
        },
        Attendees = attendees,
        IsOnlineMeeting = true,
        OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness
    };

    _logger.LogInformation(
        "Creating Teams meeting. Subject: {Subject}, StartUtc: {StartUtc}, EndUtc: {EndUtc}",
        dto.Subject,
        dto.StartUtc,
        dto.EndUtc);

    var created = await _client.Users[hostEmail].Events.PostAsync(ev);

    if (created?.OnlineMeeting?.JoinUrl == null)
    {
        _logger.LogError(
            "Meeting creation failed. HostEmail: {HostEmail}",
            hostEmail);

        throw new Exception("Meeting creation failed");
    }

    var joinUrl = created.OnlineMeeting.JoinUrl;

    _logger.LogInformation(
        "Teams meeting created successfully. EventId: {EventId}",
        created.Id);

    // 3️⃣ Fetch company name using attendee email
    var recipientEmail = dto.AttendeeEmails.First();

    _logger.LogInformation(
        "Fetching company name for recipient email: {RecipientEmail}",
        recipientEmail);

    var companyName = await GetCompanyNameByEmailAsync(recipientEmail);

    if (string.IsNullOrWhiteSpace(companyName))
    {
        _logger.LogWarning(
            "Company name not found for email {RecipientEmail}. Using fallback value.",
            recipientEmail);

        companyName = "Your Company";
    }

    _logger.LogInformation(
        "Company name resolved as: {CompanyName}",
        companyName);

    // 4️⃣ Build email body
    var emailBody = $@"
        <html>
        <body style='font-family:Segoe UI,Arial; color:#333; line-height:1.6;'>

        <p>Dear {companyName} Team,</p>

        <p>
        Thank you for booking a meeting with <strong>Westgate IT Hub (PVT) Ltd</strong>.
        We appreciate your time and look forward to our discussion.
        </p>

        <p>
        Your meeting has been successfully scheduled. Please find the meeting details
        below and use the link provided to join at the scheduled time.
        </p>

        <hr/>

        <h3>Meeting Details</h3>

        <p>
        <strong>Platform:</strong> Microsoft Teams<br/>
        <strong>Join Meeting Link:</strong><br/>
        <a href='{joinUrl}'>{joinUrl}</a>
        </p>

        <p>
        We kindly request you to join the meeting a few minutes before the scheduled time
        to ensure a smooth start.
        </p>

        <p>
        If you are unable to attend the meeting as planned, please inform us at least
        30 minutes in advance (preferably 1 hour prior) so we can make appropriate arrangements.
        </p>

        <p>
        Should you face any issues joining the meeting or require further assistance,
        please feel free to reply to this email.
        </p>

        <br/>

        <p>We look forward to speaking with you.</p>

        <p>
        Kind regards,<br/>
        <strong>Westgate IT Hub (PVT) Ltd</strong><br/>
        Ayush Kumar<br/>
        Director<br/>
        <a href='mailto:ayush@westgateithub.com'>ayush@westgateithub.com</a>
        </p>

        </body>
        </html>";

    _logger.LogInformation("Email body constructed successfully.");

    // 5️⃣ Send email
    var message = new Message
    {
        Subject = $"Meeting Confirmation – {companyName}",
        Body = new ItemBody
        {
            ContentType = BodyType.Html,
            Content = emailBody
        },
        ToRecipients = dto.AttendeeEmails.Select(email => new Recipient
        {
            EmailAddress = new EmailAddress { Address = email }
        }).ToList()
    };

    try
    {
        _logger.LogInformation(
            "Sending email from noreply@westgateithub.com to {RecipientCount} recipients.",
            dto.AttendeeEmails.Count);

        await _client.Users["noreply@westgateithub.com"]
            .SendMail
            .PostAsync(new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            });

        _logger.LogInformation("Email sent successfully.");
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Meeting created but email sending failed. EventId: {EventId}",
            created.Id);
    }

    // 6️⃣ Update DB
    _logger.LogInformation(
        "Updating next meeting time in DB. CompanyId: {CompanyId}, StartUtc: {StartUtc}",
        dto.CompanyId,
        dto.StartUtc);

    await _repo.UpdateNextMeetingAsync(dto.CompanyId, dto.StartUtc);

    _logger.LogInformation(
        "Next meeting time updated successfully for CompanyId: {CompanyId}",
        dto.CompanyId);

    // 7️⃣ Return result
    _logger.LogInformation(
        "ScheduleMeetingAsync completed successfully. EventId: {EventId}",
        created.Id);

    return new ScheduleMeetingResultDto
    {
        EventId = created.Id!,
        JoinUrl = joinUrl,
        StartUtc = dto.StartUtc,
        EndUtc = dto.EndUtc
    };
}

    
    public async Task SyncAdminCalendarAsync(string adminEmail)
    {
        var lastSync = await _repo.GetLastSyncAsync(adminEmail);
        if (lastSync.HasValue &&
            lastSync > DateTime.UtcNow.AddMinutes(-5))
        {
            return;
        }

        var startUtc = DateTime.UtcNow.Date;
        var endUtc = startUtc.AddDays(30);

        var graphEvents = await GetEventsAsync(
            adminEmail,
            startUtc,
            endUtc);

        // 🔥 MAP HERE
        var dbEvents = graphEvents.Select(e => new AdminCalendarEventDto
        {
            EventId = Guid.NewGuid().ToString(), // or Graph event id if available
            Subject = e.Subject,
            StartUtc = e.StartUtc,
            EndUtc = e.EndUtc
        }).ToList();

        await _repo.ReplaceAdminEventsAsync(adminEmail, dbEvents);
    }
    
    
    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(
        string adminEmail,
        DateTime dateIst)
    {
        if (dateIst.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return new();

        // Work hours in IST
        var workStartIst = dateIst.Date.AddHours(10);
        var workEndIst   = dateIst.Date.AddHours(17);

        // Convert work hours to UTC
        var workStartUtc = TimeZoneInfo.ConvertTimeToUtc(workStartIst, IstZone);
        var workEndUtc   = TimeZoneInfo.ConvertTimeToUtc(workEndIst, IstZone);

        // Fetch full day (UTC)
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dateIst.Date, IstZone);
        var dayEndUtc   = dayStartUtc.AddDays(1);

        var events = await _repo.GetAdminEventsAsync(
            adminEmail, dayStartUtc, dayEndUtc);

        var busyUtc = events
            .Select(e => (e.StartUtc.UtcDateTime, e.EndUtc.UtcDateTime))
            .Where(b => b.Item2 > workStartUtc && b.Item1 < workEndUtc)
            .ToList();

        var slots = new List<AvailableSlotDto>();

        var slotStartUtc = workStartUtc;
        while (slotStartUtc.AddHours(1) <= workEndUtc)
        {
            var slotEndUtc = slotStartUtc.AddHours(1);

            var overlaps = busyUtc.Any(b =>
                b.Item1 < slotEndUtc &&
                b.Item2 > slotStartUtc);

            if (!overlaps)
            {
                slots.Add(new AvailableSlotDto
                {
                    StartIst = TimeZoneInfo.ConvertTimeFromUtc(slotStartUtc, IstZone),
                    EndIst   = TimeZoneInfo.ConvertTimeFromUtc(slotEndUtc, IstZone)
                });
            }

            slotStartUtc = slotStartUtc.AddHours(1);
        }

        return slots;
    }



    private static List<(DateTime start, DateTime end)> GetFreeRanges(
        DateTime workStart,
        DateTime workEnd,
        List<(DateTime Start, DateTime End)> busy)
    {
        var free = new List<(DateTime, DateTime)>();
        var cursor = workStart;

        foreach (var b in busy)
        {
            if (b.Start > cursor)
                free.Add((cursor, b.Start));

            if (b.End > cursor)
                cursor = b.End;
        }

        if (cursor < workEnd)
            free.Add((cursor, workEnd));

        return free;
    }

    private static List<AvailableSlotDto> ExtractOneHourSlots(
        List<(DateTime start, DateTime end)> ranges)
    {
        var slots = new List<AvailableSlotDto>();

        foreach (var r in ranges)
        {
            var cursor = r.start;

            while (cursor.AddHours(1) <= r.end)
            {
                slots.Add(new AvailableSlotDto
                {
                    StartIst = cursor,
                    EndIst = cursor.AddHours(1)
                });

                cursor = cursor.AddHours(1);
            }
        }

        return slots;
    }
    
    
    private Dictionary<string, string> BuildEmailVariables(
        string recipientName,
        string hostEmail,
        ScheduleMeetingDto dto,
        string joinUrl)
    {
        return new Dictionary<string, string>
        {
            ["RecipientName"] = recipientName,
            ["Subject"] = dto.Subject,
            ["Date"] = dto.StartUtc.ToString("dd MMM yyyy"),
            ["Time"] = $"{dto.StartUtc:hh:mm tt} - {dto.EndUtc:hh:mm tt} (UTC)",
            ["HostEmail"] = hostEmail,
            ["JoinUrl"] = joinUrl
        };
    }

    
    
    public async Task<string?> GetCompanyNameByEmailAsync(string email)
    {
        const string sql = @"
        SELECT c.company_name
        FROM company_contacts cc
        JOIN companies c ON c.id = cc.company_id
        WHERE cc.email = @Email
        LIMIT 1;
    ";

        return await _dbConnection.QueryFirstOrDefaultAsync<string>(
            sql,
            new { Email = email }
        );
    }



}
