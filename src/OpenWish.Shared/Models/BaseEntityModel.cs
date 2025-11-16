namespace OpenWish.Shared.Models;

public class BaseEntityModel
{
    public int Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public byte[]? RowVersion { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;
}