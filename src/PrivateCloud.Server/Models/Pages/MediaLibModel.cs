using AutoMapper;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Models.Pages;

public class MediaLibMap : Profile
{
    public MediaLibMap()
    {
        CreateMap<MediaLibEntity, MediaLibDto>();
    }
}

public class MediaLibQueryRequest : PageRequest
{
    public string Name { get; set; }
}

public class MediaLibAddRequest : NameRequest
{
    public string Path { get; set; }
    public string AllowedRoles { get; set; }
    public string Thumb { get; set; }
}

public class MediaLibModifyRequest : NameRequest
{
    public string AllowedRoles { get; set; }
    public string Thumb { get; set; }
}

public class MediaLibCryptoRequest
{
    public string Password { get; set; }
    public string NewPassword { get; set; }
}

public class MediaLibTokenRequest
{
    public Guid Id { get; set; }
    public string Token { get; set; }
}

public class MediaLibDto : IdNameDto<Guid>
{
    public string Path { get; set; }
    public string CreateTime { get; set; }
    public string IdPath => new IdPath(new MediaLibEntity { Id = Id, Path = Path }, Path, true).Value;
    public string AllowedRoles { get; set; }
    public bool IsEncrypt { get; set; }
    public string Thumb { get; set; }
    public int RunningTaskCount { get; set; }
    public int FailedTaskCount { get; set; }
}