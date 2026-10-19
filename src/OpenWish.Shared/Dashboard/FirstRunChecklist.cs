namespace OpenWish.Shared.Dashboard;

public sealed record FirstRunChecklistStep(string Key, string Title, string Description, string Href, string ActionText, bool IsDone);

/// <summary>The dismissible "get started" checklist shown on a new account's dashboard.</summary>
public sealed record FirstRunChecklist(bool HasWishlist, bool HasIdea, bool HasFriend, bool HasEvent, bool IsDismissed)
{
    public static readonly FirstRunChecklist Hidden = new(true, true, true, true, true);

    public IReadOnlyList<FirstRunChecklistStep> Steps =>
    [
        new("wishlist", "Create a wishlist", "A home for the gifts you'd love to get.", "/wishlists/new", "Create wishlist", HasWishlist),
        new("idea", "Add your first idea", "Paste a store link or type a note.", "/add", "Add idea", HasIdea),
        new("friend", "Add a friend", "Invite someone you already know.", "/friends", "Find friends", HasFriend),
        new("event", "Join or create an event", "Plan a gift exchange or an occasion.", "/events/new", "Create event", HasEvent)
    ];

    public int CompletedCount => Steps.Count(step => step.IsDone);

    public bool IsComplete => CompletedCount == Steps.Count;

    /// <summary>Hidden once dismissed or once every step is done, so it never nags an established account.</summary>
    public bool IsVisible => !IsDismissed && !IsComplete;
}