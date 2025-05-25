namespace OpenWish.Application.Models.Configuration;

public record OpenAISettings
{
    public string? ApiKey { get; set; } = null!;
}