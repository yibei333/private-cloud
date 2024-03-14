namespace PrivateCloud.Server.Exceptions;

public class NameExisteException : Exception
{
    public NameExisteException() : base("名称已存在")
    {

    }
}