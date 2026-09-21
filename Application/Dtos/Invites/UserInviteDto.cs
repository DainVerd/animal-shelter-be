using Domain.Enums;

namespace Application.Dtos.Invites;

public class UserInviteDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public InviteStatus Status { get; set; }
    public InviteUserDto InvitedBy { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public InviteUserDto? AcceptedUser { get; set; }
}
