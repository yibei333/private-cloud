using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PrivateCloud.Server.Exceptions;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Filters;

public class ExceptionFilter(ILogger<FailFilter> logger) : IExceptionFilter
{
    private readonly ILogger<FailFilter> _logger = logger;

    public void OnException(ExceptionContext context)
    {
        var message = context.Exception.InnerException?.Message ?? context.Exception.Message;
        _logger.LogError(context.Exception, "{message}", message);
        context.Result = new JsonResult(Result.Failed(message));
        context.HttpContext.Response.StatusCode = 500;
        if (context.Exception is MediaLibUnAuthorizedException) context.HttpContext.Response.StatusCode = 402;
    }
}