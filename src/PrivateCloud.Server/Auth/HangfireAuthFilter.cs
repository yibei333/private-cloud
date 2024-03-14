using Hangfire.Dashboard;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Extensions.Jwt;
using System.Security.Claims;

namespace PrivateCloud.Server.Auth;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (IsFontRequest(context)) return true;
        var claims = context.GetHttpContext().User?.Claims?.ToList() ?? [];
        var roles = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? GetRolesFromRefer(context);
        if (roles.StringArrayMatch(StaticNames.AdminName).Count != 0) return true;
        return false;
    }

    private static string GetRolesFromRefer(DashboardContext context)
    {
        try
        {
            var httpContext = context.GetHttpContext();
            if (httpContext.Request.Headers.TryGetValue("Referer", out var referer))
            {
                var token = new Uri(referer).ParseQueryString()[StaticNames.TokenSchemeName];
                var jwtService = httpContext.RequestServices.GetRequiredService<IJwtService>();
                var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();

                var jwtResult = jwtService.Verify(new JwtVerifyOption(token, configuration.GetValue<string>(StaticNames.JwtKeyName)));
                if (jwtResult.IsVerified)
                {
                    var payload = jwtResult.Payload.DeSerialize<LocalPaylod>();
                    if (payload.NotNull() && payload.Expire > DateTime.Now.ToUtcTimestamp()) return payload.Roles;
                }
            }
        }
        catch { }
        return null;
    }

    private static readonly List<string> _fontEndpoints = ["/woff", "/woff2", "/ttf"];
    private static bool IsFontRequest(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var path = httpContext.Request.Path.ToString();
        return _fontEndpoints.Any(x => path.EndsWith(x));
    }
}