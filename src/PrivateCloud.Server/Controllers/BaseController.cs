using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Encryption;
using SharpDevLib.Extensions.Jwt;
using System.Security.Claims;
using System.Text;

namespace PrivateCloud.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    protected readonly IMapper _mapper;
    protected readonly IConfiguration _configuration;
    protected readonly IEncryption _encryption;
    protected readonly IEncryptionTransform<AesEncryptOption, AesDecryptOption> _aes;
    protected readonly IJwtService _jwtService;
    protected readonly IRepository<MediaLibEntity> _mediaLibRepository;
    private bool _userSeted;
    private LocalPaylod _currentUser;

    public BaseController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        _mapper = serviceProvider.GetRequiredService<IMapper>();
        _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _encryption = serviceProvider.GetRequiredService<IEncryption>();
        _aes = _encryption.Symmetric.Aes;
        _jwtService = serviceProvider.GetRequiredService<IJwtService>();
        _mediaLibRepository = serviceProvider.GetRequiredService<IRepository<MediaLibEntity>>();
    }

    protected LocalPaylod CurrentUser
    {
        get
        {
            if (_userSeted) return _currentUser;
            var claims = HttpContext.User?.Claims?.ToList() ?? [];
            _currentUser = new LocalPaylod
            {
                Name = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                Id = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value.ToGuid() ?? Guid.Empty,
                Roles = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value,
                CryptoId = claims.FirstOrDefault(x => x.Type == nameof(LocalPaylod.CryptoId))?.Value,
            };
            _userSeted = true;
            return _currentUser;
        }
    }

    protected bool VideoThumbIsGridImage => _configuration.GetValue<bool?>(StaticNames.VideoThumbIsGridImageName) ?? false;

    protected IdPath BuildIdPathModel(string idPath, out MediaLibEntity mediaLib)
    {
        var idPathModel = new IdPath(idPath);
        mediaLib = _mediaLibRepository.Get(x => x.Id == idPathModel.MediaLibId) ?? throw new MediaLibNotFoundException();
        EnsureMediaLibAuth(mediaLib);
        return idPathModel;
    }

    protected void EnsureMediaLibAuth(MediaLibEntity mediaLib)
    {
        if (mediaLib.AllowedRoles.NotEmpty())
        {
            if (mediaLib.AllowedRoles.StringArrayMatch(CurrentUser?.Roles).Count == 0) throw new MediaLibUnAuthorizedException();
        }

        if (mediaLib.IsEncrypt)
        {
            var token = HttpContext.GetValueFromHeaderOrQueryStringOrCookie(StaticNames.MediaLibTokenHeaderName);
            if (token.IsEmpty()) throw new MediaLibUnAuthorizedException();

            var jwtKey = _configuration.GetValue<string>(StaticNames.JwtKeyName);
            var verifyResult = _jwtService.Verify(new JwtVerifyOption(token, jwtKey));
            if (!verifyResult.IsVerified) throw new MediaLibUnAuthorizedException();

            var payload = verifyResult.Payload?.DeSerialize<MediaLibPayload>() ?? null;
            if (payload is null || payload.MediaLibId != mediaLib.Id) throw new MediaLibUnAuthorizedException();

            var key = Encoding.UTF8.GetString(_encryption.Symmetric.Aes.Decrypt(payload.Key, new AesDecryptOption(jwtKey, CryptoExtension.ZeroAesIVBtyes)));
            if (key != mediaLib.EncryptedKey) throw new MediaLibUnAuthorizedException();
        }
    }

    protected FileResult BuildFileStreamResult(string fileName, Stream stream, bool tryOpen = false)
    {
        var contentType = fileName.GetMimeType();
        if (!contentType.Contains(";charset=")) contentType += ";charset=utf-8";
        Response.Headers.Append("Content-Disposition", $"inline;filename={fileName.UrlEncode()}");
        if (tryOpen) return File(stream, contentType, true);
        return File(stream, contentType, fileName, true);
    }

    protected FileResult BuildFileResult(string fileName, byte[] bytes)
    {
        var contentType = fileName.GetMimeType();
        if (!contentType.Contains(";charset=")) contentType += ";charset=utf-8";
        Response.Headers.Append("Content-Disposition", $"inline;filename={fileName}");
        var result = File(bytes, contentType, fileName, true);
        return result;
    }
}
