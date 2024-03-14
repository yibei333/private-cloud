using PrivateCloud.Server.Models;
using SharpDevLib;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivateCloud.Server.Data.Entity;

public class CryptoTaskEntity
{
    public CryptoTaskEntity()
    {
        CreateTime = DateTime.Now.ToUtcTimestamp();
        TaskId = Guid.NewGuid();
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid MediaLibId { get; set; }
    public CryptoTaskType Type { get; set; }
    public string FullName { get; set; }
    public bool IsFolder { get; set; }
    public long CreateTime { get; set; }
    public string Message { get; set; }
    public int HandleCount { get; set; }
    public Guid TaskId { get; set; }

    [NotMapped]
    public int Deepth { get; set; }
}
