namespace OpenWish.Data.Entities;

public class Notification : BaseEntity
{
    public string Message { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsRead { get; set; }
    public int UserId { get; set; }
    public OpenWishUser OpenWishUser { get; set; }
}