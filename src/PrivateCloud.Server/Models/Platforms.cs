namespace PrivateCloud.Server.Models;

public enum Platforms
{
    android = 1,
    ios,
    mac,
    tizen,
    windows,
}

public static class PlatformExtension
{
    public static Platforms ToPlatform(this string platform)
    {
        return Enum.TryParse(platform, out Platforms platforms) ? platforms : throw new NotSupportedException($"platform '{platform}' not supported");
    }
}
