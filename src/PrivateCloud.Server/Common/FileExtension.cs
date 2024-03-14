using HeyRed.Mime;
using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class FileExtension
{
    private static readonly double _kbUnit = 1024;
    private static readonly double _mbUnit = 1024 * _kbUnit;
    private static readonly double _gbUnit = 1024 * _mbUnit;
    private static readonly double _tbUnit = 1024 * _gbUnit;

    public static string GetSize(this long size)
    {
        if (size > _tbUnit) return $"{(Math.Round(size / _tbUnit, 2))}TB";
        else if (size > _gbUnit) return $"{(Math.Round(size / _gbUnit, 2))}GB";
        else if (size > _mbUnit) return $"{(Math.Round(size / _mbUnit, 2))}MB";
        else if (size > _kbUnit) return $"{(Math.Round(size / _kbUnit, 2))}KB";
        else return $"{size}Byte";
    }

    public static string GetMimeType(this string fileName) => MimeTypesMap.GetMimeType(fileName);

    public static string GetIcon(this string extension)
    {
        return extension switch
        {
            "apk" => "apk",
            "json" or "js" or "css" or "html" or "java" or "python" or "go" or "assembly" => "code",
            "cs" => "csharp",
            "xls" or "xlsx" or "csv" => "excel",
            "exe" or "msbuddle" => "exe",
            "iso" => "iso",
            "mp3" or "wav" or "wma" or "mp2" or "flac" or "midi" or "ra" or "ape" or "aac" or "cda" => "mp3",
            "pdf" => "pdf",
            "jpg" or "png" or "bmp" or "tif" or "gif" or "pcx" or "tga" or "exif" or "fpx" or "svg" or "psd" or "cdr" or "pcd" or "dxf" or "ufo" or "eps" or "ai" or "raw" or "wmf" or "webp" or "avif" or "apng" => "picture",
            "ppt" or "pptx" => "ppt",
            "ps1" or "bat" or "sh" => "shell",
            "text" or "txt" or "ini" or "config" or "md" => "txt",
            "avi" or "mp4" or "dat" or "dvr" or "vcd" or "mov" or "svcd" or "vob" or "dvd" or "dvtr" or "bbc" or "evd" or "flv" or "rm" or "rmvb" or "wmv" or "mkv" or "3gp" => "video",
            "doc" or "docx" => "word",
            "zip" or "rar" or "7z" or "tar.gz" or "tar" or "gz" or "bz2" or "xz" or "tgz" => "zip",
            "jar" => "jar",
            _ => "file",
        };
    }

    public static bool IsGif(this string fileName) => fileName.GetFileExtension() == "gif";

    private static readonly List<string> _imageExtensions = ["jpg", "png", "jpeg", "tga", "pbm", "tiff", "bmp", "webp", "gif"];
    public static bool IsImage(this string fileName) => _imageExtensions.Contains(fileName.GetFileExtension().ToLower());

    private static readonly List<string> _videoExtensions = ["avi", "mp4", "dat", "dvr", "vcd", "mov", "svcd", "vob", "dvd", "dvtr", "bbc", "evd", "flv", "rm", "rmvb", "wmv", "mkv", "3gp",];
    public static bool IsVideo(this string fileName) => _videoExtensions.Contains(fileName.GetFileExtension().ToLower());

    private static readonly Dictionary<string, List<string>> _playTypes = new Dictionary<string, List<string>> {
        { "txt",new List<string>{ "json" , "js" , "css" , "html" , "java" , "python" , "go","cs", "text" , "txt" , "ini" , "config" , "md" } },
        { "img",new List<string>{ "jpg","jpeg","png","gif","bmp" } },
        { "video",new List<string>{ "mp4","m3u8"} },
        { "audio",new List<string>{ "mp3" } },
    };
    public static string PlayType(this string fileName)
    {
        var extension = fileName.GetFileExtension().ToLower();
        var type = _playTypes.Where(x => x.Value.Contains(extension));
        return type.Any() ? type.First().Key : string.Empty;
    }

    private static readonly List<string> _staticFilesExtensions = ["html", "js", "css"];
    public static bool IsStaticFiles(this string name) => _staticFilesExtensions.Contains(name.GetFileExtension().ToLower());

    public static string FormatPath(this string path) => path.Replace("\\", "/").TrimEnd('/');

    public static string GetFileName(this string fileName) => new FileInfo(fileName).Name;
}