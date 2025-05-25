namespace OpenWish.Application.Models.Configuration;

public record EmailConfigSettings
{
    public string? SmtpHost { get; set; } = null!;
    public int? SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; } = null!;
    public string? SmtpPass { get; set; } = null!;
    public string? SmtpFrom { get; set; } = null!;
}