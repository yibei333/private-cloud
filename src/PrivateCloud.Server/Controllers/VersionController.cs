using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib.Extensions.Http;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

public class VersionController(IServiceProvider serviceProvider, IConfiguration configuration) : BaseController(serviceProvider)
{
    [HttpGet]
    [Route("last/{platform}")]
    public async Task<Result<VersionReply>> GetLast(string platform)
    {
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
