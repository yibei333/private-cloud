namespace PrivateCloud.Server.Exceptions;

public class MediaLibNotFoundException : Exception
{
    public MediaLibNotFoundException() : base("媒体库不存在")
    {

    }
}