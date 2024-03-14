using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class Statics
{
    public static CancellationTokenSource AppCancellationTokenSource { get; } = new();
    public static IServiceProvider ServiceProvider { get; set; }
    public static readonly string RootPath = AppDomain.CurrentDomain.BaseDirectory.CombinePath("data");
    public static readonly string DBPath = RootPath.CombinePath("db");
    public static readonly string DBFilePath = DBPath.CombinePath("database.db");
    public static readonly string DbConnectionString = $"Data Source={DBFilePath}";
    public static readonly string FfmpegPath = RootPath.CombinePath("ffmpeg");
    public static readonly string TempPath = RootPath.CombinePath("temp");
    public static readonly string UserPath = RootPath.CombinePath("user");
    public static readonly string AdminPasswordPath = UserPath.CombinePath("AdminPassword.txt");
    public static readonly string LogPath = UserPath.CombinePath("log.txt");
    public static readonly string HangfireRoute = "/hangfire";

    public static void Init()
    {
        RootPath.EnsureDirectoryExist();
        DBPath.EnsureDirectoryExist();
        FfmpegPath.EnsureDirectoryExist();
        TempPath.EnsureDirectoryExist();
        UserPath.EnsureDirectoryExist();
    }

    public static string GenerateAesIV() => Guid.NewGuid().ToString().Replace("-", "")[..16];
    public const long BigFileSize = 1024 * 1024 * 1024;
    public const int TaskMaxHandleCount = 5;
}