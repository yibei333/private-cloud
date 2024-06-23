using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Models.Pages;

public class GetEntriesRequest : PageRequest
{
    public string IdPath { get; set; }
    public string Name { get; set; }
    public string SortField { get; set; }
    public string SortDescending { get; set; }
}

public class CreateFolderRequest : NameRequest
{
    public string IdPath { get; set; }
}

public class RenameRequest : NameRequest
{
    public bool IsFolder { get; set; }
}

public class FolderReply : IdNameRequest
{
    public FolderReply(MediaLibEntity mediaLib, IdPath idPathModel)
    {
        MediaLibId = mediaLib.Id;
        MediaLibName = mediaLib.Name;
        MediaLibIdPath = new IdPath(mediaLib, mediaLib.Path, true).Value;
        IsEncrypt = mediaLib.IsEncrypt;
        Id = idPathModel.Name.ToGuid();
        Name = idPathModel.Name;
        IdPath = idPathModel.Value;
        IsRoot = idPathModel.RelativePath.IsEmpty();
    }

    public Guid MediaLibId { get; }
    public string MediaLibName { get; }
    public string MediaLibIdPath { get; }
    public string IdPath { get; }
    public bool IsRoot { get; }
    public bool IsEncrypt { get; }
    public List<FileParentReply> Parents { get; } = [];
}

public class FileParentReply : IdNameRequest
{
    public FileParentReply(IdPath idPathModel)
    {
        Id = idPathModel.Name.ToGuid();
        Name = idPathModel.Name;
        IdPath = idPathModel.Value;
        MediaLibId = idPathModel.MediaLibId;
    }

    public string IdPath { get; }
    public Guid MediaLibId { get; }
}

public class FileReply
{
    public int Total { get; set; }
    public int Index { get; set; }
    public bool HasNext => Next is not null;
    public bool HasPre => Pre is not null;
    public EntryReply Current { get; set; }
    public EntryReply Next { get; set; }
    public EntryReply Pre { get; set; }
}

public class EntryReply : IdNameRequest
{
    public EntryReply(IdPath idPathModel)
    {
        var fileInfo = new FileInfo(idPathModel.AbsolutePath);
        var directoryInfo = new DirectoryInfo(idPathModel.AbsolutePath);

        Key = Guid.NewGuid();
        Id = idPathModel.Name.ToGuid();
        Name = idPathModel.Name;
        MediaLibId = idPathModel.MediaLibId;
        IsFolder = idPathModel.IsFolder;
        IdPath = idPathModel.Value;
        Time = (IsFolder ? directoryInfo.LastWriteTime : fileInfo.LastWriteTime).ToUtcTimestamp().ToLocalTimeString();
        Size = IsFolder ? 0 : fileInfo.Length;
        IsEncrypt = idPathModel.IsEncrypt;
        RelativePath = idPathModel.RelativePath;
        AbsolutePath = idPathModel.AbsolutePath;
    }

    public Guid Key { get; }
    public Guid MediaLibId { get; }
    public string IdPath { get; }
    public string ParentIdPath { get; set; }
    public string Time { get; }
    public long Size { get; }
    public string SizeText => Size.GetSize();
    public string Type => IsFolder ? string.Empty : Name.GetFileExtension();
    public string PlayType => IsFolder ? "" : (IsEncrypt && Name.IsVideo() ? "video" : Name.PlayType());
    public bool IsFavorited => FavoritedId.NotEmpty();
    public bool BigFile => Size >= Statics.BigFileSize;
    public bool IsEncrypt { get; }
    public bool Encrypting { get; set; }
    public Guid FavoritedId { get; set; }
    public bool IsFolder { get; }
    public string Icon => IsFolder ? "folder" : Type.GetIcon();
    public bool HasThumb { get; set; }
    public bool HasLargeThumb { get; set; }
    public string Position { get; set; }
    public string RelativePath { get; set; }
    public string AbsolutePath { get; set; }
}

public class NameIdPathReply
{
    public string Name { get; set; }
    public string IdPath { get; set; }
}
