using Domain.Enums;

namespace Application.ViewModels;

public class UserInviteFilterViewModel
{
    public string? EmailSearchText { get; set; } 
    public string? RoleInclude { get; set; } 
    public DateTimeOffset? ExpiresFrom { get; set; }
    public DateTimeOffset? ExpiresTo { get; set; }
    public DateTimeOffset? AcceptedFrom { get; set; }
    public DateTimeOffset? AcceptedTo { get; set; }
    public InviteStatus? Status { get; set; }
    public int? InvitedByUserId { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
}
