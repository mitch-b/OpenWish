using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenWish.Shared.Integrations;

// Wire contract for /api/integrations/v1. Documented in .docs/INTEGRATIONS.md; change additively.

public sealed class IntegrationWishlistModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Icon { get; init; }
    public bool IsDefault { get; init; }
    public string? Url { get; set; }
}

public sealed class IntegrationItemModel
{
    public required string Id { get; init; }
    public string? ExternalId { get; init; }
    public required string WishlistId { get; init; }
    public required string WishlistName { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? ImageUrl { get; init; }
    public decimal? Price { get; init; }
    public string? WhereToBuy { get; init; }
    public int? Priority { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset UpdatedOn { get; init; }
    public string? OpenWishUrl { get; set; }
}

public sealed class IntegrationProfileModel
{
    public required string ConnectionId { get; init; }
    public required IntegrationProfileAppModel App { get; init; }
    public required IntegrationProfileUserModel User { get; init; }
    public DateTimeOffset ConnectedOn { get; init; }
    public IReadOnlyList<IntegrationWishlistModel> Wishlists { get; init; } = [];
}

public sealed record IntegrationProfileAppModel(string ClientId, string Name);

public sealed record IntegrationProfileUserModel(string DisplayName);

public sealed class IntegrationItemRequest : IValidatableObject
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int ExternalIdMaxLength = 200;
    public const int WhereToBuyMaxLength = 200;
    public const int UrlMaxLength = 2048;

    public string? WishlistId { get; set; }

    [MaxLength(ExternalIdMaxLength)]
    public string? ExternalId { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(NameMaxLength)]
    public string? Name { get; set; }

    [MaxLength(DescriptionMaxLength)]
    public string? Description { get; set; }

    [MaxLength(UrlMaxLength)]
    public string? Url { get; set; }

    [MaxLength(UrlMaxLength)]
    public string? ImageUrl { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Price { get; set; }

    [MaxLength(WhereToBuyMaxLength)]
    public string? WhereToBuy { get; set; }

    /// <summary>1 high, 2 medium, 3 low. Null or absent keeps the current priority on update; 0 clears it.</summary>
    [Range(0, 3)]
    public int? Priority { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
        }

        if (!IsHttpUrl(Url))
        {
            yield return new ValidationResult("Url must be an absolute http or https URL.", [nameof(Url)]);
        }

        if (!IsHttpUrl(ImageUrl))
        {
            yield return new ValidationResult("ImageUrl must be an absolute http or https URL.", [nameof(ImageUrl)]);
        }

        if (Price.HasValue && decimal.Round(Price.Value, 2) != Price.Value)
        {
            yield return new ValidationResult("Price cannot have more than two decimal places.", [nameof(Price)]);
        }

        if (ExternalId is not null && string.IsNullOrWhiteSpace(ExternalId))
        {
            yield return new ValidationResult("ExternalId cannot be blank.", [nameof(ExternalId)]);
        }
    }

    private static bool IsHttpUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}

public sealed class IntegrationErrorModel
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorDescription { get; init; }

    [JsonPropertyName("itemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemId { get; init; }
}

public sealed class IntegrationTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = IntegrationScopes.WishlistItems;

    [JsonPropertyName("connection_id")]
    public required string ConnectionId { get; init; }
}

public static class IntegrationScopes
{
    public const string WishlistItems = "wishlist.items";
}