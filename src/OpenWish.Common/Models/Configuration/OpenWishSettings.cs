namespace OpenWish.Common.Models.Configuration;

public record OpenWishSettings
{
    public DatabaseSettings? Database { get; set; } = null!;
    public EmailConfigSettings? EmailConfig { get; set; } = null!;
    public OpenAISettings? OpenAI { get; set; } = null!;
    public string? BaseUri { get; set; } = null!;
}