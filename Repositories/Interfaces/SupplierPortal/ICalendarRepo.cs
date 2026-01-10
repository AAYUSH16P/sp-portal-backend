using Shared;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface ICalendarRepo
{
    Task UpdateNextMeetingAsync(Guid companyId, DateTime meetingUtc);

    Task ReplaceAdminEventsAsync(
        string adminEmail,
        List<AdminCalendarEventDto> events);

    Task<DateTime?> GetLastSyncAsync(string adminEmail);
    
    Task<List<AdminCalendarEventDto>> GetAdminEventsAsync(
        string adminEmail,
        DateTime startUtc,
        DateTime endUtc);
}