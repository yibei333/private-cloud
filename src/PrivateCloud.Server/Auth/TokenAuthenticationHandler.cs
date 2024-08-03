using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Cryptography;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PrivateCloud.Server.Auth;

public class TokenAuthenticationHandler(
    IConfiguration configuration,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
{
    public async Task SignOutAsync(AuthenticationProperties properties)
    {
        await Task.CompletedTask;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var noResult = Task.FromResult(AuthenticateResult.NoResult());
        if (!Scheme.Name.Equals(StaticNames.TokenSchemeName)) return noResult;

        var token = Context.GetValueFromHeaderOrQueryStringOrCookie(StaticNames.TokenSchemeName);
        if (token.IsNullOrWhiteSpace()) return noResult;

        var jwtResult = Jwt.Verify(new JwtVerifyWithHMACSHA256Request(token, configuration.GetValue<string>(StaticNames.JwtKeyName).Utf8Decode()));
        if (!jwtResult.IsVerified) return noResult;

        var payload = jwtResult.Payload.DeSerialize<LocalPaylod>();
        if (payload is null || payload.Expire <= DateTime.Now.ToUtcTimestamp()) return noResult;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,payload.Name),
            new(ClaimTypes.NameIdentifier,payload.Id.ToString()),
            new(ClaimTypes.Role,payload.Roles??string.Empty),
            new(nameof(payload.CryptoId),payload.CryptoId),
        };
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, StaticNames.TokenSchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(userPrincipal, Scheme.Name)));
    }
}