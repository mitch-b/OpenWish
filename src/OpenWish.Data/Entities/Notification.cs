namespace OpenWish.Data.Entities;

public class Notification : BaseEntity
{
    public string Message { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? SenderUserId { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsRead { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public ApplicationUser? SenderUser { get; set; }
    public string? ActionData { get; set; }
}