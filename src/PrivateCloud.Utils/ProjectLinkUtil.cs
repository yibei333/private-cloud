using SharpDevLib;
using System.Text;

namespace PrivateCloud.Utils;

public static class ProjectLinkUtil
{
    public static string GenerateLinks(bool isMaui)
    {
        var sourceDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory.CombinePath($"../../../../PrivateCloud.Shared"));
        var exceptDirectories = new List<DirectoryInfo>();
       
        var exceptFiles = new List<FileInfo>
        {
            new(sourceDirectory.FullName.CombinePath("/services/httpService.js"))
        };

        if (isMaui)
        {
            exceptFiles.Add(new FileInfo(sourceDirectory.FullName.CombinePath("index.html")));
        }

        var builder = new StringBuilder();
        sourceDirectory.GetDirectories()
            .Where(x => !exceptDirectories.Any(y => y.FullName == x.FullName))
            .ToList()
            .ForEach(x => BuildDirectoryInfo(builder, sourceDirectory, x, exceptDirectories, exceptFiles));
        sourceDirectory.GetFiles().ToList().ForEach(x => BuildFileInfo(builder, sourceDirectory, x, exceptFiles));
        return builder.ToString();
    }

    static void BuildFileInfo(StringBuilder builder, DirectoryInfo sourceDirectory, FileInfo fileInfo, List<FileInfo> exceptFiles)
    {
        if (exceptFiles.Any(x => x.FullName == fileInfo.FullName)) return;
        var relativePath = fileInfo.FullName.Replace(sourceDirectory.FullName, "");
        builder.AppendLine($"<Content CopyToOutputDirectory=\"PreserveNewest\" Include=\"..\\PrivateCloud.Shared\\{relativePath}\" Link=\"wwwroot\\{relativePath}\" />".Replace("\\\\", "\\"));
    }

    static void BuildDirectoryInfo(StringBuilder builder, DirectoryInfo sourceDirectory, DirectoryInfo directoryInfo, List<DirectoryInfo> exceptDirectories, List<FileInfo> exceptFiles)
    {
        directoryInfo.GetDirectories()
            .Where(x => !exceptDirectories.Any(y => y.FullName == x.FullName))
            .ToList()
            .ForEach(x => BuildDirectoryInfo(builder, sourceDirectory, x, exceptDirectories, exceptFiles));
        directoryInfo.GetFiles().ToList().ForEach(x => BuildFileInfo(builder, sourceDirectory, x, exceptFiles));
    }
}
