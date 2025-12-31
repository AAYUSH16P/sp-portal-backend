namespace Shared;

public class SetPasswordDto
{
    public string CurrentPassword { get; set; }

    public string Password { get; set; } = string.Empty;
}