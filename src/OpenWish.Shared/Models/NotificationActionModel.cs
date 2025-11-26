using System;
using System.Collections.Generic;

namespace OpenWish.Shared.Models;

public class NotificationActionModel
{
    public string Type { get; set; } = string.Empty;
    public string? NavigateTo { get; set; }
    private Dictionary<string, string> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private List<NotificationActionOptionModel> _options = new();

    public Dictionary<string, string> Parameters
    {
        get => _parameters;
        set => _parameters = value is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
    }

    public List<NotificationActionOptionModel> Options
    {
        get => _options;
        set => _options = value ?? new List<NotificationActionOptionModel>();
    }
}

public class NotificationActionOptionModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}