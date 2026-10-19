namespace OpenWish.Shared.Models;

/// <summary>Who drew whom, shared with the group after the organizer reveals a gift exchange. Never carries emails or IDs.</summary>
public record RevealedPairingModel(string GiverName, string ReceiverName);