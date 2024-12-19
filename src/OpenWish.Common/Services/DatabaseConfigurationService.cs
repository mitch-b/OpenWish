using OpenWish.Common.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace OpenWish.Common.Services;

public interface IDatabaseConfigurationService
{
    string GetConnectionString();
}

public class DatabaseConfigurationService(IOptions<OpenWishSettings> openWishSettings) : IDatabaseConfigurationService
{
    private readonly OpenWishSettings _openWishSettings = openWishSettings.Value;

    public string GetConnectionString()
    {
        var stringBuilder = new StringBuilder();
        if (string.Equals(_openWishSettings.Database.DbProvider, "MSSQL", StringComparison.OrdinalIgnoreCase))
        {
            stringBuilder.Append($"Server={_openWishSettings.Database.Host};");
            stringBuilder.Append($"Database={_openWishSettings.Database.Name};");
            stringBuilder.Append($"User Id={_openWishSettings.Database.User};");
            stringBuilder.Append($"Password='{_openWishSettings.Database.Password}';");
            stringBuilder.Append($"Encrypt=Optional;"); // TODO: make this configuration option
        }
        else if (string.Equals(_openWishSettings.Database.DbProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            stringBuilder.Append($"Data Source={_openWishSettings.Database.Name};");
        }
        else
        {
            throw new InvalidOperationException("Invalid database provider.");
        }
        return stringBuilder.ToString();
    }
}