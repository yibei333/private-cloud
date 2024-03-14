namespace PrivateCloud.Server.Exceptions;

public class PathExisteException : Exception
{
    public PathExisteException() : base("路径已存在")
    {

    }
}