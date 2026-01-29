using Microsoft.AspNetCore.Http;

namespace Shared.Dtos;

public class CreateNotificationDto
{
    public string Title { get; set; }
    public string Message { get; set; }

    public string Type { get; set; }
    public string Priority { get; set; }

    public string TargetType { get; set; } // ALL | SPECIFIC
    public List<int>? SupplierIds { get; set; }

    public IFormFile? Attachment { get; set; }
}