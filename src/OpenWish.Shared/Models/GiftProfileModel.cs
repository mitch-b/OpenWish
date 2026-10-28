namespace OpenWish.Shared.Models;

/// <summary>Optional notes that help friends choose a gift. Shared with friends only.</summary>
public class GiftProfileModel
{
    public const int MaxLength = 300;

    [System.ComponentModel.DataAnnotations.StringLength(MaxLength)]
    public string? Sizes { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(MaxLength)]
    public string? Favorites { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(MaxLength)]
    public string? Avoid { get; set; }

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(Sizes) || !string.IsNullOrWhiteSpace(Favorites) || !string.IsNullOrWhiteSpace(Avoid);
}