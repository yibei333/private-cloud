namespace PrivateCloud.Server.Exceptions;

public class ParameterRequiredException(string parameterName) : Exception($"字段'{parameterName}'是必需的")
{
}