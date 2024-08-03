using SharpDevLib;

namespace PrivateCloud.Server.Data.Entity;

public abstract class BaseEntity
{
    public BaseEntity()
    {
        Id = Guid.NewGuid();
        CreateTime = DateTime.Now.ToUtcTimestamp();
    }

    public Guid Id { get; set; }
    public long CreateTime { get; set; }
}
