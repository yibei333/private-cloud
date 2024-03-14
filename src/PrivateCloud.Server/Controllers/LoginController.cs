using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Jwt;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

public class LoginController(IServiceProvider serviceProvider, IRepository<UserEntity> userRepository, IJwtService jwtService) : BaseController(serviceProvider)
{
    [HttpGet]
    [AllowAnonymous]
    [Route("notice")]
    public Result<List<string>> GetNotice()
    {
        var result = new List<string>();

        if (System.IO.File.Exists(Statics.AdminPasswordPath))
        {
            var initPassword = System.IO.File.ReadAllText(Statics.AdminPasswordPath);
            var adminUser = userRepository.Get(x => x.Name == StaticNames.AdminName);
            if (adminUser is not null)
            {
                var password = adminUser.Salt.PasswordHash(initPassword);
                if (password == adminUser.Password) result.Add($"你可以在'{Statics.AdminPasswordPath}'中找到初始用户'admin'的密码,登录成功后请修改初始密码");
            }
        }

        var ffmepgDirectory = _configuration.GetValue<string>("FfmpegBinaryPath");
        var ffmpegPath = Statics.FfmpegPath.CombinePath(ffmepgDirectory);
        if (!System.IO.File.Exists(ffmpegPath)) result.Add($"请将ffmpeg可执行文件放在路径'{ffmpegPath}'上,以保证缩略图正常工作");
        return Result.Succeed(result);
    }

    [HttpPost]
    [AllowAnonymous]
    public Result<LocalPaylod> Post([FromBody] LoginRequest request)
    {
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Password.IsEmpty()) throw new ParameterRequiredException(nameof(request.Password));

        var user = userRepository.Get(x => x.Name == request.Name) ?? throw new UserNotFoundException();
        if (user.IsForbidden) throw new UserForbiddenException();

        var password = user.Salt.PasswordHash(request.Password);
        if (password != user.Password)
        {
            user.LoginFailCount += 1;
            if (user.LoginFailCount > 5)
            {
                user.IsForbidden = true;
                userRepository.Update(user);
                throw new MaliciousRequestException();
            }
            else
            {
                userRepository.Update(user);
                throw new PasswordErrorException();
            }
        }
        if (user.LoginFailCount > 0)
        {
            user.LoginFailCount = 0;
            userRepository.Update(user);
        }

        var expire = DateTime.UtcNow.AddHours(_configuration.GetValue<int>(StaticNames.LoginExpireHourName));
        var cryptoId = Guid.NewGuid().ToString();
        var payload = new LocalPaylod(user.Id, user.Name, cryptoId, user.Roles, expire.ToUtcTimestamp(), null);
        payload.Token = jwtService.Create(new JwtCreateOption(JwtAlgorithm.HS256, _configuration.GetValue<string>(StaticNames.JwtKeyName), payload));
        return Result.Succeed(payload);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("logout")]
    public async Task<Result> PostAsync()
    {
        await HttpContext.SignOutAsync(StaticNames.TokenSchemeName);
        return Result.Succeed();
    }
}
