namespace Shared;

public class User1Dto
{
    public Guid Id { get; set; }

    public Guid? InstanceId { get; set; }

    public string? Aud { get; set; }

    public string? Role { get; set; }

    public string? Email { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public DateTime? InvitedAt { get; set; }

    public DateTime? LastSignInAt { get; set; }

    public bool? IsSuperAdmin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Phone { get; set; }

    public DateTime? PhoneConfirmedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public bool IsSsoUser { get; set; }

    public bool IsAnonymous { get; set; }

    public DateTime? DeletedAt { get; set; }
}