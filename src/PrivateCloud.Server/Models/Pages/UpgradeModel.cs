using SharpDevLib;

namespace PrivateCloud.Server.Models.Pages;

public class VersionDto(string version)
{
    public VersionDto(VersionConfig config, string version, Platforms platform) : this(version)
    {
        GiteeUrl = platform == Platforms.android ? config.GiteeAndroidUrl.Replace("%version%", version) : config.GiteeWindowsUrl.Replace("%version%", version);
        GithubUrl = platform == Platforms.android ? config.GithubAndroidUrl.Replace("%version%", version) : config.GithubWindowsUrl.Replace("%version%", version);
        Name = GiteeUrl.GetFileName();
    }

    public string Version { get; } = version;
    public string GiteeUrl { get; }
    public string GithubUrl { get; }
    public string Name { get; }
}

public class VersionConfig
{
    public string GithubVersionUrl { get; set; }
    public string GithubAndroidUrl { get; set; }
    public string GithubWindowsUrl { get; set; }
    public string GiteeVersionUrl { get; set; }
    public string GiteeAndroidUrl { get; set; }
    public string GiteeWindowsUrl { get; set; }
}