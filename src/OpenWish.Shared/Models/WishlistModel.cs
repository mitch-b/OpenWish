using System.Text.Json.Serialization;

namespace OpenWish.Shared.Models;

public class WishlistModel : BaseEntityModel
{
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public string? OwnerId { get; set; }
    public ApplicationUserModel? Owner { get; set; }
    [JsonIgnore]
    public int? EventId { get; set; }
    public bool IsCollaborative { get; set; }
    public int ItemCount { get; set; }
    public ICollection<WishlistItemModel> Items { get; set; } = [];
    public ICollection<WishlistPermissionModel> Permissions { get; set; } = [];
    public bool IsPrivate { get; set; }
    public bool IsFriendsOnly { get; set; } // When true, only friends with explicit permissions can see (not all friends)
}