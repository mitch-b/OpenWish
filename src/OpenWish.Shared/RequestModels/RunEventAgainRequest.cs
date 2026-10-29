namespace OpenWish.Shared.RequestModels;

public class RunEventAgainRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
}