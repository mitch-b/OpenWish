namespace OpenWish.Common.Models.Configuration;

public record DatabaseSettings
{
    public string? DbProvider { get; set; } = null!;
    public string? Host { get; set; } = null!;
    public string? Name { get; set; } = null!;
    public string? User { get; set; } = null!;
    public string? Password { get; set; } = null!;
}