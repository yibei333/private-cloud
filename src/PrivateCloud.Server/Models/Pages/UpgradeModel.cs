using AutoMapper;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Models.Pages;

public class UpgradeMap : Profile
{
    public UpgradeMap()
    {
        CreateMap<UpgradeEntity, UpgradeReply>();
    }
}

public class UpgradeQueryRequest : PageRequest
{
    public string Version { get; set; }
}

public class UpgradeAddRequest
{
    public string Version { get; set; }
    public string Url { get; set; }
    public string LocalUrl { get; set; }
    public Platforms Platform { get; set; }
}

public class UpgradeModifyRequest
{
    public string Version { get; set; }
    public string Url { get; set; }
    public string LocalUrl { get; set; }
    public Platforms Platform { get; set; }
}

public class UpgradeReply : IdDto
{
    public string Version { get; set; }
    public string Url { get; set; }
    public string LocalUrl { get; set; }
    public string CreateTime { get; set; }
    public Platforms Platform { get; set; }
    public string PlatformName => Platform.ToString();
    public string Extension => Platform.GetExtension();
}