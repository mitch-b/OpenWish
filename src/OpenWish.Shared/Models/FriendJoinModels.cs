namespace OpenWish.Shared.Models;

/// <summary>A freshly generated in-person connection code. The code is only ever returned once, at creation.</summary>
public record FriendJoinCodeModel
{
    public string Code { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }

    /// <summary>SVG markup of a QR code encoding <see cref="JoinUrl"/>.</summary>
    public string QrSvg { get; set; } = string.Empty;
}

public enum FriendJoinStatus
{
    Valid,
    AlreadyFriends,
    OwnCode,
    Invalid,
    Connected
}

/// <summary>What a code holder may learn before confirming: only the owner's public display name.</summary>
public record FriendJoinPreviewModel
{
    public FriendJoinStatus Status { get; set; }
    public string? DisplayName { get; set; }
}

public record FriendJoinRequestModel
{
    public string Code { get; set; } = string.Empty;
}