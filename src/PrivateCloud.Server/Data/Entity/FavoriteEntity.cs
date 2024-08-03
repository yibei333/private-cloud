namespace PrivateCloud.Server.Data.Entity;

public class FavoriteEntity : BaseEntity
{
    public FavoriteEntity()
    {
    }

    public Guid MediaLibId { get; set; }
    public string IdPath { get; set; }
    public Guid UserId { get; set; }
    public bool IsFolder { get; set; }
    public string Name { get; set; }
}
