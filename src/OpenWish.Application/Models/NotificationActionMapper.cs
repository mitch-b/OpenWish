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

        return JsonSerializer.Serialize(action, _serializerOptions);
    }
}