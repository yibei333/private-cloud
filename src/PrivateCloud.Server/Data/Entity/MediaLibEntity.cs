namespace PrivateCloud.Server.Data.Entity;

public class MediaLibEntity : BaseEntity
{
    public MediaLibEntity()
    {
    }

    public MediaLibEntity(string name, string path, string allowedRoles, string thumb)
    {
        Name = name;
        Path = path;
        AllowedRoles = allowedRoles;
        Thumb = thumb;
    }

    public string Name { get; set; }
    public string Path { get; set; }
    public string AllowedRoles { get; set; }
    public string Thumb { get; set; }
    public bool IsEncrypt { get; set; }
    public string EncryptedKey { get; set; }
}
