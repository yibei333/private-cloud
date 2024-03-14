namespace PrivateCloud.Server.Exceptions;

public class PasswordErrorException : Exception
{
    public PasswordErrorException() : base("密码错误")
    {

    }
}