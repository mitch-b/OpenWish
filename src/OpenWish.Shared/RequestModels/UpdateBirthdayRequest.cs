namespace OpenWish.Shared.RequestModels;

public sealed class UpdateBirthdayRequest
{
    /// <summary>Leave both null to stop sharing a birthday.</summary>
    public int? Month { get; init; }
    public int? Day { get; init; }
}