namespace OpenWish.Application.Integrations;

internal static class IntegrationWishlistDefaults
{
    public static int? Resolve(int? configuredId, IReadOnlyCollection<int> allowedIds) =>
        configuredId.HasValue && allowedIds.Contains(configuredId.Value)
            ? configuredId
            : allowedIds.Count > 0 ? allowedIds.Min() : null;
}