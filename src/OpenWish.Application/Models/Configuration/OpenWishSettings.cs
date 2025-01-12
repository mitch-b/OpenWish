namespace OpenWish.Application.Models.Configuration;

public record OpenWishSettings
{
    public EmailConfigSettings? EmailConfig { get; set; } = null!;
    public OpenAISettings? OpenAI { get; set; } = null!;
    public string? AppVersion { get; set; } = null!;
    public string? BaseUri { get; set; } = null!;
    public bool OwnDatabaseUpgrades { get; set; }
}