namespace PrivateCloud.Server.Data.Entity;

public class ThumbTaskEntity : BaseEntity
{
    public ThumbTaskEntity()
    {
    }

    public string IdPath { get; set; }
    public int HandledCount { get; set; }
}
