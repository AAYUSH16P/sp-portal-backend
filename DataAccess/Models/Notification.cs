namespace FinancialManagementDataAccess.Models;

public class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; }
    public string Message { get; set; }

    public string Type { get; set; }
    public string Priority { get; set; }

    public string TargetType { get; set; }
    public string Status { get; set; }

    public string? AttachmentName { get; set; }
    public byte[]? AttachmentContent { get; set; }
    public string? AttachmentMime { get; set; }

    public int CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
}