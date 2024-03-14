using SharpDevLib.Extensions.Data;

namespace PrivateCloud.Server.Data.Entity;

public class ForeverRecordEntity : BaseEntity
{
    public ForeverRecordEntity()
    {
    }

    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public Guid Salt { get; set; }
    public string Signature { get; set; }
}
