using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrivateCloud.Server.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MultipartFormDataAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.HasFormContentType && context.HttpContext.Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase)) return;
        context.Result = new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
    }
}