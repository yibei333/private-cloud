using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Http;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

public class VersionController(IServiceProvider serviceProvider, IConfiguration configuration) : BaseController(serviceProvider)
{
    [HttpGet]
    [Route("last/{platform}")]
    public async Task<Result<VersionReply>> GetLast(string platform)
    {
        if (platform == "web")
        {
            var path = AppDomain.CurrentDomain.BaseDirectory.CombinePath("version.txt");
            var serverVersion = "1.0";
            if(System.IO.File.Exists(path)) serverVersion = System.IO.File.ReadAllText(path);
            return Result.Succeed(new VersionReply(serverVersion));
        }

        var platformEnum = platform.ToPlatform();
        if (platformEnum != Platforms.android && platformEnum != Platforms.windows) throw new Exception("当前只支持windows和android客户端");

        var config = new VersionConfig();
        configuration.GetSection("Version").Bind(config);
        var httpService = _serviceProvider.GetRequiredService<IHttpService>();
        var version = await httpService.GetAsync<string>(new ParameterOption(config.GiteeVersionUrl));
        if (!version.IsSuccess) version = await httpService.GetAsync<string>(new ParameterOption(config.GithubVersionUrl));
        if (!version.IsSuccess) return Result.Failed<VersionReply>(version.Message);
        return Result.Succeed(new VersionReply(config, version.Data, platformEnum));
    }
}
