using AutoMapper;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Models.Pages;

public class ForeverRecordMap : Profile
{
    public ForeverRecordMap()
    {
        CreateMap<ForeverRecordEntity, ForeverRecordDto>();
    }
}

public class ForeverRecordRequest : PageRequest
{
    public string Name { get; set; }
}

public class ForeverRecordDto : IdDto<Guid>
{
    public long CreateTime { get; set; }
    public string Time => CreateTime.ToUtcTime().ToTimeString();
    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public string Signature { get; set; }
    public string Name => new IdPath(IdPath).Name;
}