namespace OpenWish.Data.Entities;

/// <summary>A named group of the owner's friends, such as Family or Book Club. Private to its owner.</summary>
public class Circle : BaseEntity
{
    public required string OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public required string Name { get; set; }

    public ICollection<CircleMember> Members { get; set; } = [];
    public ICollection<CircleWishlist> Wishlists { get; set; } = [];
}