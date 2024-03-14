using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Models;

public class LocalPaylod : IdNameDto
{
    public LocalPaylod()
    {

    }

    public LocalPaylod(Guid id, string name, string cryptoId, string roles, long expire, string token)
    {
        Id = id;
        Name = name;
        CryptoId = cryptoId;
        Roles = roles;
        Expire = expire;
        Token = token;
    }

    public string CryptoId { get; set; }
    public string Roles { get; set; }
    public long Expire { get; set; }
    public string Token { get; set; }
}