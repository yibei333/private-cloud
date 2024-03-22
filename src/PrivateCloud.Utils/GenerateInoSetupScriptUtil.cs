using SharpDevLib;
using System.Text;

namespace PrivateCloud.Utils;

public static class GenerateInoSetupScriptUtil
{
    public static string Generate()
    {
        var builder = new StringBuilder();
        var path = AppDomain.CurrentDomain.BaseDirectory.CombinePath($"../../../../PrivateCloud.Maui/bin/packages/windows/publish/PrivateCloud.Maui/release_net8.0-windows10.0.19041.0");
        Console.WriteLine(path);
        var sourceDirectory = new DirectoryInfo(path);
        if (sourceDirectory.Exists)
        {
            sourceDirectory.GetDirectories().ToList().ForEach(x => {
                builder.AppendLine($"Source: \"{{#MyBinaryFolder}}\\{x.Name}\\*\"; DestDir: \"{{app}}\"; Flags: ignoreversion recursesubdirs createallsubdirs");
            });
            sourceDirectory.GetFiles().ToList().ForEach(x => {
                builder.AppendLine($"Source: \"{{#MyBinaryFolder}}\\{x.Name}\"; DestDir: \"{{app}}\"; Flags: ignoreversion");
            });
        }
        return builder.ToString();
    }
}