namespace Shared.Dtos;
using Microsoft.AspNetCore.Http;

public class UpdateNotificationDto
{
    public int NotificationId { get; set; }

    public string Title { get; set; }
    public string Message { get; set; }

    public string Type { get; set; }
    public string Priority { get; set; }

    public string TargetType { get; set; } // ALL | SPECIFIC
    public List<Guid>? SupplierIds { get; set; }

    public IFormFile? Attachment { get; set; }
    public bool RemoveAttachment { get; set; }
}
