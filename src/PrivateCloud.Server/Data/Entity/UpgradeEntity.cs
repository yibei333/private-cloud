using PrivateCloud.Server.Models;
using SharpDevLib.Extensions.Data;

namespace PrivateCloud.Server.Data.Entity;

public class UpgradeEntity : BaseEntity
{
    public string Version { get; set; }
    public string Url { get; set; }
    public string LocalUrl { get; set; }
    public Platforms Platform { get; set; }
}