namespace PrivateCloud.Server.Exceptions;

public class MediaLibUnAuthorizedException : Exception
{
    public MediaLibUnAuthorizedException() : base("没有媒体库权限")
    {

    }
}