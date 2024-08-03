using AutoMapper;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Models.Pages;

public class FavoriteMap : Profile
{
    public FavoriteMap()
    {
        CreateMap<FavoriteEntity, FavoriteDto>();
    }
}

public class FavoriteQueryRequest : PageRequest
{
    public string Name { get; set; }
}

public class FavoriteAddRequest
{
    public bool IsFolder { get; set; }
    public string Name { get; set; }
}

public class FavoriteDto : IdNameRequest<Guid>
{
    public long CreateTime { get; set; }
    public string Time => CreateTime.ToUtcTime().ToTimeString();
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public bool IsFolder { get; set; }
    public string Icon => IsFolder ? "folder" : Name.GetFileExtension(false).GetIcon();
    public bool HasThumb { get; set; }
    public bool HasLargeThumb { get; set; }
    public bool IsEncrypt { get; set; }
    public Guid EncryptId { get; set; }
    public Guid MediaLibId { get; set; }
    public string MediaLibName { get; set; }
}