namespace FinancialManagementDataAccess.Models;

public class NotificationRead
{
    public int NotificationId { get; set; }
    public int SupplierId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}