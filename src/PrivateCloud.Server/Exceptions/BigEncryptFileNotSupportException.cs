namespace PrivateCloud.Server.Exceptions;

public class BigEncryptFileNotSupportException : Exception
{
    public BigEncryptFileNotSupportException() : base("不支持加密大文件")
    {

    }
}

public class EncryptFileNotSupportException : Exception
{
    public EncryptFileNotSupportException() : base("不支持加密文件")
    {

    }
}