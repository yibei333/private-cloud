namespace PrivateCloud.Server.Data.Entity;

public class EncryptedFileEntity : BaseEntity
{
    public EncryptedFileEntity()
    {
    }

    public Guid MediaLibId { get; set; }
    public string Key { get; set; }
    public string IV { get; set; }
    public string Name { get; set; }
}