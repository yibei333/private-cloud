using AutoMapper;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Models.Pages;

public class UserMap : Profile
{
    public UserMap()
    {
        CreateMap<UserEntity, UserDto>();
    }
}

public class UserQueryRequest : PageRequest
{
    public string Name { get; set; }
}

public class UserAddRequest : NameRequest
{
    public string Password { get; set; }
    public string Roles { get; set; }
}

public class UserModifyRequest : NameRequest
{
    public string Password { get; set; }
    public string Roles { get; set; }
    public bool IsForbidden { get; set; }
}

public class UserDto : IdNameDto<Guid>
{
    public string Roles { get; set; }
    public string CreateTime { get; set; }
    public bool IsForbidden { get; set; }
}