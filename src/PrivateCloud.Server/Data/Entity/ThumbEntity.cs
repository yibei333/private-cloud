using SharpDevLib.Extensions.Data;

namespace PrivateCloud.Server.Data.Entity;

public class ThumbEntity : BaseEntity
{
    public ThumbEntity()
    {
    }

    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public DateTime FileLastModifyTime { get; set; }
}
