using System.Text.Json;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Models;

internal static class NotificationActionMapper
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public static NotificationActionModel? Deserialize(string? actionData)
    {
        if (string.IsNullOrWhiteSpace(actionData))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<NotificationActionModel>(actionData, _serializerOptions);
        }
        catch
        {
            return null;
        }
    }

    public static string? Serialize(NotificationActionModel? action)
    {
        if (action is null)
        {
            return null;
        }

        if (action.NavigateTo is not null &&
            (!action.NavigateTo.StartsWith("/", StringComparison.Ordinal) ||
             action.NavigateTo.StartsWith("//", StringComparison.Ordinal) ||
             action.NavigateTo.Contains(':', StringComparison.Ordinal)))
        {
            throw new ArgumentException("Notification navigation targets must be relative application paths.", nameof(action));
        }

        return JsonSerializer.Serialize(action, _serializerOptions);
    }
}