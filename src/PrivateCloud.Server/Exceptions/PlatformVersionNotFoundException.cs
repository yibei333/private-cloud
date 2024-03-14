namespace PrivateCloud.Server.Exceptions;

public class PlatformVersionNotFoundException : Exception
{
    public PlatformVersionNotFoundException() : base("找不到平台版本信息")
    {

    }
}