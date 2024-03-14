namespace PrivateCloud.Server.Exceptions;

public class PathNotFoundException : Exception
{
    public PathNotFoundException() : base("路径不存在")
    {

    }
}