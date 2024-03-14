namespace PrivateCloud.Server.Exceptions;

public class PlatformVersionExistException : Exception
{
    public PlatformVersionExistException() : base("平台版本已存在")
    {

    }
}