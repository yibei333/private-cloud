using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Models;

public class IdPath
{
    public IdPath(string value)
    {
        Value = value;
        var array = value.Trim().HexStringDecode().Utf8Encode().Split(";");
        MediaLibId = array[0].ToGuid();
        IsFolder = array[1].ToBoolean();
        MediaLibPath = array[2].FormatPath();
        RelativePath = array[3].FormatPath();
        IsEncrypt = array[4].ToBoolean();
        AbsolutePath = MediaLibPath.CombinePath(RelativePath).FormatPath();
    }

    public IdPath(MediaLibEntity mediaLib, string fullPath, bool isFolder)
    {
        MediaLibId = mediaLib.Id;
        IsFolder = isFolder;
        IsEncrypt = mediaLib.IsEncrypt;
        MediaLibPath = mediaLib.Path.FormatPath();
        AbsolutePath = fullPath.FormatPath();
        RelativePath = new DirectoryInfo(fullPath).FullName.Replace(new DirectoryInfo(mediaLib.Path).FullName, "").FormatPath();
        Value = $"{mediaLib.Id};{isFolder};{mediaLib.Path};{RelativePath};{mediaLib.IsEncrypt}".Utf8Decode().HexStringEncode();
    }

    public string Value { get; }
    public Guid MediaLibId { get; }
    public string MediaLibPath { get; }
    public string AbsolutePath { get; }
    public string RelativePath { get; }
    public bool IsFolder { get; }
    public bool IsEncrypt { get; }
    public string Name => IsFolder ? new DirectoryInfo(AbsolutePath).Name : new FileInfo(AbsolutePath).Name;

    public string GetThumbPath(int type, bool isGridImage, DataContext dataContext)
    {
        var id = Name;
        var extension = ".encrypted";
        if (!IsEncrypt)
        {
            var thumb = dataContext.Thumb.FirstOrDefault(x => x.IdPath == Value);
            if (thumb is null) return null;
            id = thumb.Id.ToString();
            extension = string.Empty;
        }

        return type switch
        {
            0 => Statics.TempPath.CombinePath($"{id}/{id}.png{extension}"),
            1 => isGridImage ? Statics.TempPath.CombinePath($"{id}/{id}.grid.png{extension}") : Statics.TempPath.CombinePath($"{id}/{id}.gif{extension}"),
            _ => throw new NotSupportedException(),
        };
    }
}