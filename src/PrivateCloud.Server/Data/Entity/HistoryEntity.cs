namespace PrivateCloud.Server.Data.Entity;

public class HistoryEntity : BaseEntity
{
    public HistoryEntity()
    {
    }

    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
}
