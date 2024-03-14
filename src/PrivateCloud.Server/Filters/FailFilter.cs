using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpDevLib;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Filters;

public class FailFilter(ILogger<FailFilter> logger) : IResultFilter
{
    private readonly ILogger<FailFilter> _logger = logger;

    public void OnResultExecuted(ResultExecutedContext context)
    {

    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var result = (context.Result as ObjectResult)?.Value as Result;
        if (result.NotNull() && !result!.Success)
        {
            context.HttpContext.Response.StatusCode = 500;
            _logger.LogError("result failed:{error}", result.Description);
        }
    }
}
