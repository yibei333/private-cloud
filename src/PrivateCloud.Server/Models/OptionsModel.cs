namespace PrivateCloud.Server.Models;

public class OptionsModel
{
    public OptionsModel()
    {

    }
    public OptionsModel(string name, List<KeyValueModel> items)
    {
        Name = name;
        Options = items;
    }

    public string Name { get; set; }
    public List<KeyValueModel> Options { get; set; }
}