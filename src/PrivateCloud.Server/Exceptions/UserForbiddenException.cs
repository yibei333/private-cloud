namespace PrivateCloud.Server.Exceptions;

public class UserForbiddenException : Exception
{
    public UserForbiddenException() : base("用户被禁用")
    {

    }
}