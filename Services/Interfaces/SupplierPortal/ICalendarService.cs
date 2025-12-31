using Shared;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetEventsAsync(
        string hostEmail,
        DateTime startUtc,
        DateTime endUtc
    );

    Task<ScheduleMeetingResultDto> ScheduleMeetingAsync(
        string hostEmail,
        ScheduleMeetingDto dto
    );


    Task SyncAdminCalendarAsync(string adminEmail);
    
    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(
        string adminEmail,
        DateTime dateIst);
}