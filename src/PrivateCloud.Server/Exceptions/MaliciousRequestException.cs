namespace PrivateCloud.Server.Exceptions;

public class MaliciousRequestException : Exception
{
    public MaliciousRequestException() : base("恶意请求")
    {

    }
}