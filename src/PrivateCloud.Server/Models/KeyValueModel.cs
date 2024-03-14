namespace PrivateCloud.Server.Models;

public class KeyValueModel
{
    public KeyValueModel()
    {

    }

    public KeyValueModel(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }
    public object Value { get; set; }
}
