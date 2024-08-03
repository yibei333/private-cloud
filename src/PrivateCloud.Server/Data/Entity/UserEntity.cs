namespace PrivateCloud.Server.Data.Entity;

public class UserEntity : BaseEntity
{
    public UserEntity()
    {
    }

    public UserEntity(string name, string salt, string password, string roles)
    {
        Name = name;
        Salt = salt;
        Password = password;
        Roles = roles;
    }

    public string Name { get; set; }
    public string Salt { get; set; }
    public string Password { get; set; }
    public string Roles { get; set; }
    public bool IsForbidden { get; set; }
    public int LoginFailCount { get; set; }
}
