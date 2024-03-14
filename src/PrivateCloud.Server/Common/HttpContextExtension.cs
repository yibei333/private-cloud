using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class HttpContextExtension
{
    public static string GetValueFromHeaderOrQueryStringOrCookie(this HttpContext context, string name)
    {
        var header = context.Request.Headers.TryGetValue(name, out var hValue) ? hValue.ToString() : null;
        if (header.NotEmpty()) return header;

        header = context.Request.Query.ContainsKey(name) ? context.Request.Query[name].ToString() : null;
        if (header.NotEmpty()) return header;

        return context.Request.Cookies.TryGetValue(name, out var cValue) ? cValue : null;
    }
}