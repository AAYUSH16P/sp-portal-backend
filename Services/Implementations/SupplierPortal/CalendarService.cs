using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Shared;
using Shared.Dtos;
using Shared.Enums;

public class CalendarService : ICalendarService
{
    private readonly GraphServiceClient _client;
    private readonly ICalendarRepo _repo;
    private readonly IEmailService _emailService;
    
    private static readonly TimeZoneInfo IstZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "India Standard Time"
                : "Asia/Kolkata"
        );


    public CalendarService(IConfiguration config, ICalendarRepo repo, IEmailService emailService)
    {
        _client = GraphClientFactory.Create(
            config["AzureAd:TenantId"]!,
            config["AzureAd:ClientId"]!,
            config["AzureAd:ClientSecret"]!
        );
        _repo = repo;
        _emailService = emailService;
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
        var attendees = dto.AttendeeEmails.Select(email =>
            new Attendee
            {
                EmailAddress = new EmailAddress { Address = email },
                Type = AttendeeType.Required
            }).ToList();

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

        var created = await _client.Users[hostEmail].Events.PostAsync(ev);

        var joinUrl = created!.OnlineMeeting?.JoinUrl!;

        //
        // await _emailService.SendAsync(new SendEmailRequestDto
        // {
        //     To = hostEmail,
        //     TemplateType = EmailTemplateType.MeetingScheduled,
        //     Variables = BuildEmailVariables(
        //         "Admin",
        //         hostEmail,
        //         dto,
        //         joinUrl)
        // });

        // 📧 Send email to attendees
        // foreach (var email in dto.AttendeeEmails)
        // {
        //     await _emailService.SendAsync(new SendEmailRequestDto
        //     {
        //         To = email,
        //         TemplateType = EmailTemplateType.MeetingScheduled,
        //         Variables = BuildEmailVariables(
        //             "Participant",
        //             hostEmail,
        //             dto,
        //             joinUrl)
        //     });
        // }

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



}
