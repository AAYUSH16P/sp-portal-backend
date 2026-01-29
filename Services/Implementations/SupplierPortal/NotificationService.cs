
using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using FinancialManagementDataAccess.Models;
using Shared.Dtos;

namespace Services.Implementations.SupplierPortal;



public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task CreateAsync(CreateNotificationDto dto, Guid adminId)
    {
        byte[]? fileBytes = null;
        string? mime = null;

        if (dto.Attachment != null)
        {
            fileBytes = await FileHelper.ToByteArrayAsync(dto.Attachment);
            mime = dto.Attachment.ContentType;
        }

        var notification = new Notification
        {
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            Priority = dto.Priority,
            TargetType = dto.TargetType,
            AttachmentName = dto.Attachment?.FileName,
            AttachmentContent = fileBytes,
            AttachmentMime = mime,
            CreatedByAdminId = adminId
        };

        var id = await _repo.CreateAsync(notification);

        if (dto.TargetType == "SPECIFIC" && dto.SupplierIds?.Any() == true)
        {
            await _repo.AddTargetsAsync(id, dto.SupplierIds);
        }
    }
    
    
    
    

    public async Task UpdateAsync(UpdateNotificationDto dto, Guid adminId)
    {
        var existing = await _repo.GetByIdAsync(dto.NotificationId);

        if (existing == null)
            throw new Exception("Notification not found");

        if (existing.Status != "DRAFT")
            throw new Exception("Only draft notifications can be edited");

        byte[]? attachmentBytes = existing.AttachmentContent;
        string? attachmentName = existing.AttachmentName;
        string? attachmentMime = existing.AttachmentMime;

        if (dto.RemoveAttachment)
        {
            attachmentBytes = null;
            attachmentName = null;
            attachmentMime = null;
        }
        else if (dto.Attachment != null)
        {
            attachmentBytes = await FileHelper.ToByteArrayAsync(dto.Attachment);
            attachmentName = dto.Attachment.FileName;
            attachmentMime = dto.Attachment.ContentType;
        }

        var updated = new Notification
        {
            NotificationId = dto.NotificationId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            Priority = dto.Priority,
            TargetType = dto.TargetType,

            AttachmentName = attachmentName,
            AttachmentContent = attachmentBytes,
            AttachmentMime = attachmentMime,
            UpdatedByAdminId = adminId
        };

        await _repo.UpdateAsync(updated);

        // 🔥 Update targets
        await _repo.RemoveTargetsAsync(dto.NotificationId);

        if (dto.TargetType == "SPECIFIC" && dto.SupplierIds?.Any() == true)
        {
            await _repo.AddTargetsAsync(dto.NotificationId, dto.SupplierIds);
        }
    }
    
    
    public async Task SendAsync(int notificationId, Guid adminId)
    {
        var notification = await _repo.GetByIdAsync(notificationId);

        if (notification == null)
            throw new Exception("Notification not found");

        if (notification.Status != "DRAFT")
            throw new Exception("Only draft notifications can be sent");

        await _repo.SendAsync(notificationId, adminId);
    }

    public async Task DeleteAsync(int notificationId, Guid adminId)
    {
        var notification = await _repo.GetByIdAsync(notificationId);

        if (notification == null)
            throw new Exception("Notification not found");

        await _repo.DeleteAsync(notificationId, adminId);
    }


}


