namespace OpenWish.Shared.Models;

public class NotificationModel : BaseEntityModel
{
    public string Message { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? SenderUserId { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsRead { get; set; }
    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }
    public ApplicationUserModel? SenderUser { get; set; }
    public NotificationActionModel? Action { get; set; }
}