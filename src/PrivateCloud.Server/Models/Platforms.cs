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

    public static string GetExtension(this Platforms platform)
    {
        if (platform == Platforms.windows) return "msix";
        if (platform == Platforms.android) return "apk";
        if (platform == Platforms.ios) return "ipa";
        if (platform == Platforms.mac) return "pkg";
        if (platform == Platforms.tizen) return "wgt";
        return string.Empty;
    }
}
