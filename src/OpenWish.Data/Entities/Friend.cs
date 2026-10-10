namespace OpenWish.Data.Entities;

public class Friend : BaseEntity
{
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required string FriendUserId { get; set; }
    public ApplicationUser? FriendUser { get; set; }

    public DateTimeOffset FriendshipDate { get; set; }
}