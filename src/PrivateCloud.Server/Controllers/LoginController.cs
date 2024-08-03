using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Cryptography;

namespace PrivateCloud.Server.Controllers;

public class LoginController(IServiceProvider serviceProvider) : BaseController(serviceProvider)
{
    [HttpGet]
    [AllowAnonymous]
    [Route("notice")]
    public DataReply<List<string>> GetNotice()
    {
        var result = new List<string>();

        if (System.IO.File.Exists(Statics.AdminPasswordPath))
        {
            var initPassword = System.IO.File.ReadAllText(Statics.AdminPasswordPath);
            var adminUser = _dbContext.User.FirstOrDefault(x => x.Name == StaticNames.AdminName);
            if (adminUser is not null)
            {
                var password = adminUser.Salt.PasswordHash(initPassword);
                if (password == adminUser.Password) result.Add($"你可以在'{Statics.AdminPasswordPath}'中找到初始用户'Admin'的密码,登录成功后请修改初始密码");
            }
        }
        return DataReply<List<string>>.Succeed(result);
    }

    [HttpPost]
    [AllowAnonymous]
    public DataReply<LocalPaylod> Post([FromBody] LoginRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Password.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Password));

        var user = _dbContext.User.FirstOrDefault(x => x.Name == request.Name) ?? throw new UserNotFoundException();
        if (user.IsForbidden) throw new UserForbiddenException();

        var password = user.Salt.PasswordHash(request.Password);
        if (password != user.Password)
        {
            user.LoginFailCount += 1;
            if (user.LoginFailCount > 5)
            {
                user.IsForbidden = true;
                _dbContext.User.Update(user);
                _dbContext.SaveChanges();
                throw new MaliciousRequestException();
            }
            else
            {
                _dbContext.User.Update(user);
                _dbContext.SaveChanges();
                throw new PasswordErrorException();
            }
        }
        if (user.LoginFailCount > 0)
        {
            user.LoginFailCount = 0;
            _dbContext.User.Update(user);
            _dbContext.SaveChanges();
        }

        var expire = DateTime.UtcNow.AddHours(_configuration.GetValue<int>(StaticNames.LoginExpireHourName));
        var cryptoId = Guid.NewGuid().ToString();
        var payload = new LocalPaylod(user.Id, user.Name, cryptoId, user.Roles, expire.ToUtcTimestamp(), null);
        payload.Token = Jwt.Create(new JwtCreateWithHMACSHA256Request(payload, _configuration.GetValue<string>(StaticNames.JwtKeyName).Utf8Decode()));
        return DataReply<LocalPaylod>.Succeed(payload);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("logout")]
    public async Task<EmptyReply> PostAsync()
    {
        await HttpContext.SignOutAsync(StaticNames.TokenSchemeName);
        return EmptyReply.Succeed();
    }
}
