namespace OpenWish.Shared.Models;

public sealed record ReleaseEntryModel(
    string Version,
    string Date,
    string Title,
    string Summary,
    IReadOnlyList<string> Highlights);