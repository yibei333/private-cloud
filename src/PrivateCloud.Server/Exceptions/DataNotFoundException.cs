namespace PrivateCloud.Server.Exceptions;

public class DataNotFoundException : Exception
{
    public DataNotFoundException() : base("找不到数据")
    {

    }
}

public class DataNotFoundOrHandlingException : Exception
{
    public DataNotFoundOrHandlingException() : base("找不到数据或在处理中")
    {

    }
}