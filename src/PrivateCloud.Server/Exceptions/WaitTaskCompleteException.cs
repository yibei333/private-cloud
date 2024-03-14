namespace PrivateCloud.Server.Exceptions;

public class WaitTaskCompleteException : Exception
{
    public WaitTaskCompleteException() : base("请等待其他任务完成")
    {

    }
}