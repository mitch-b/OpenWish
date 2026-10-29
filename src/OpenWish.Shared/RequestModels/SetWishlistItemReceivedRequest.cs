using System.ComponentModel.DataAnnotations;

namespace OpenWish.Shared.RequestModels;

public sealed class SetWishlistItemReceivedRequest
{
    public const int MaxThankYouLength = 300;

    public bool IsReceived { get; init; }

    [MaxLength(MaxThankYouLength)]
    public string? ThankYouMessage { get; init; }
}