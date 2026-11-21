namespace OpenWish.Data.Entities;

public class CircleMember : BaseEntity
{
    public int CircleId { get; set; }
    public Circle? Circle { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
}