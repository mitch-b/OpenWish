namespace OpenWish.Shared.Models;

/// <summary>A person the caller already shares an event or wishlist with but is not yet connected to.</summary>
public record FriendSuggestionModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Why this person is suggested, such as "Both in Holiday Exchange".</summary>
    public string Reason { get; set; } = string.Empty;
}