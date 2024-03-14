namespace PrivateCloud.Server.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("用户不存在")
    {

    }
}