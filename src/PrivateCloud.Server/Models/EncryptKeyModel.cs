namespace PrivateCloud.Server.Models;

public class EncryptKeyModel(string key, string iV)
{
    public string Key { get; } = key;
    public string IV { get; } = iV;
}
