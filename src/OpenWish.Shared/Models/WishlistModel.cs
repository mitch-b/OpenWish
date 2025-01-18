namespace OpenWish.Shared.Models;

public class WishlistModel : BaseEntityModel
{
    public required string Name { get; set; }
    public string? OwnerId { get; set; }
    public ApplicationUserModel? Owner { get; set; }
    public bool IsCollaborative { get; set; }
    public int ItemCount { get; set; }
    public ICollection<WishlistItemModel> Items { get; set; } = [];
}
