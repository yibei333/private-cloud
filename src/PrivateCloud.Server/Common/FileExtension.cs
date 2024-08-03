using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class FileExtension
{
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

    public static bool IsGif(this string fileName) => fileName.GetFileExtension(false) == "gif";

    private static readonly List<string> _imageExtensions = ["jpg", "png", "jpeg", "tga", "pbm", "tiff", "bmp", "webp", "gif"];
    public static bool IsImage(this string fileName) => _imageExtensions.Contains(fileName.GetFileExtension(false).ToLower());

    private static readonly List<string> _videoExtensions = ["avi", "mp4", "dat", "dvr", "vcd", "mov", "svcd", "vob", "dvd", "dvtr", "bbc", "evd", "flv", "rm", "rmvb", "wmv", "mkv", "3gp",];
    public static bool IsVideo(this string fileName) => _videoExtensions.Contains(fileName.GetFileExtension(false).ToLower());

    private static readonly Dictionary<string, List<string>> _playTypes = new()
    {
        { "txt",new List<string>{ "json" , "js" , "css" , "html" , "java" , "python" , "go","cs", "text" , "txt" , "ini" , "config" , "md" } },
        { "img",new List<string>{ "jpg","jpeg","png","gif","bmp" } },
        { "video",new List<string>{ "mp4","m3u8"} },
        { "audio",new List<string>{ "mp3" } },
    };
    public static string PlayType(this string fileName)
    {
        var extension = fileName.GetFileExtension(false).ToLower();
        var type = _playTypes.Where(x => x.Value.Contains(extension));
        return type.Any() ? type.First().Key : string.Empty;
    }

    private static readonly List<string> _staticFilesExtensions = ["html", "js", "css"];
    public static bool IsStaticFiles(this string name) => _staticFilesExtensions.Contains(name.GetFileExtension(false).ToLower());
}