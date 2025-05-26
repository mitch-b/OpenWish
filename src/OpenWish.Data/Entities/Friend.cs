namespace OpenWish.Data.Entities;

public class Friend : BaseEntity
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public string FriendUserId { get; set; }
    public ApplicationUser FriendUser { get; set; }
    
    public DateTimeOffset FriendshipDate { get; set; }
}