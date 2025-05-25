using Microsoft.AspNetCore.Components;

namespace OpenWish.Web.Client.CustomEvents;

[EventHandler("onopenwishpaste", typeof(OpenWishPasteEventArgs),
    enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}

public class OpenWishPasteEventArgs : EventArgs
{
    public DateTime EventTimestamp { get; set; }
    public string? PastedData { get; set; }
}