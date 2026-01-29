using Application.Interfaces;
using DynamicFormRepo.DynamicFormRepoInterface;
using FinancialManagementDataAccess.Models;

namespace Application.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly IUserNotificationRepository _repo;

    public UserNotificationService(IUserNotificationRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<Notification>> GetNotificationsAsync(int supplierId)
        => _repo.GetNotificationsAsync(supplierId);

    public Task<Notification> GetByIdAsync(int id, int supplierId)
        => _repo.GetByIdAsync(id, supplierId);

    public Task MarkAsReadAsync(int id, int supplierId)
        => _repo.MarkAsReadAsync(id, supplierId);

    public Task<int> GetUnreadCountAsync(int supplierId)
        => _repo.GetUnreadCountAsync(supplierId);

    public Task<(byte[] content, string mime, string name)> GetAttachmentAsync(int id)
        => _repo.GetAttachmentAsync(id);
}