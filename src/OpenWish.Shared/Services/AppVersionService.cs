namespace OpenWish.Shared.Services;

public interface IAppVersionService
{
    string GetAppVersion();
}

public class AppVersionService(string versionNumber) : IAppVersionService
{
    public string GetAppVersion()
    {
        return versionNumber;
    }
}
