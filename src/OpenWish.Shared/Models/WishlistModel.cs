namespace OpenWish.Shared.Models;

public class WishlistModel : BaseEntityModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string OwnerId { get; set; }
    public ApplicationUserModel? Owner { get; set; }
    public bool IsCollaborative { get; set; }
    public int ItemCount { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public ICollection<WishlistItemModel> Items { get; set; } = [];
}
