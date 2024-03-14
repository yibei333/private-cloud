using AutoMapper;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Models.Pages;

public class ForeverRecordMap : Profile
{
    public ForeverRecordMap()
    {
        CreateMap<ForeverRecordEntity, ForeverRecordReply>();
    }
}

public class ForeverRecordRequest : PageRequest
{
    public string Name { get; set; }
}

public class ForeverRecordReply : IdRequest
{
    public long CreateTime { get; set; }
    public string Time => CreateTime.ToLocalTimeString();
    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public string Signature { get; set; }
    public string Name => new IdPath(IdPath).Name;
}