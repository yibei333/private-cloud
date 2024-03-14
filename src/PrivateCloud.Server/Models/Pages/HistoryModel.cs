using AutoMapper;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Models.Pages;

public class HistoryMap : Profile
{
    public HistoryMap()
    {
        CreateMap<HistoryEntity, HistoryReply>();
    }
}

public class HistoryQueryRequest : PageRequest
{
    public string Name { get; set; }
    public DateTime? StartTime { get; set; }
    public long? StartTimeLong => StartTime?.ToUtcTimestamp();
    public DateTime? EndTime { get; set; }
    public long? EndTimeLong => EndTime?.AddDays(1).ToUtcTimestamp();
}

public class HistoryAddRequest : NameRequest
{
    public string Position { get; set; }
}

public class HistoryReply : IdNameRequest
{
    public long CreateTime { get; set; }
    public string Time => CreateTime.ToLocalTimeString();
    public string IdPath { get; set; }
    public string Position { get; set; }
    public bool IsFolder { get; set; }
    public string Icon => IsFolder ? "folder" : Name.GetFileExtension().GetIcon();
    public bool HasThumb { get; set; }
    public bool HasLargeThumb { get; set; }
    public bool IsEncrypt { get; set; }
    public Guid EncryptId { get; set; }
    public Guid MediaLibId { get; set; }
    public string MediaLibName { get; set; }
}