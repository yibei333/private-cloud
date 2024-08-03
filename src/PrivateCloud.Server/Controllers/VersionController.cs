using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Transport;

namespace PrivateCloud.Server.Controllers;

public class VersionController(IServiceProvider serviceProvider, IConfiguration configuration) : BaseController(serviceProvider)
{
    [HttpGet]
    [Route("last/{platform}")]
    public async Task<DataReply<VersionDto>> GetLast(string platform)
    {
        if (platform == "web")
        {
            var path = AppDomain.CurrentDomain.BaseDirectory.CombinePath("version.txt");
            var serverVersion = "1.0";
            if (System.IO.File.Exists(path)) serverVersion = System.IO.File.ReadAllText(path);
            return DataReply<VersionDto>.Succeed(new VersionDto(serverVersion));
        }

        var platformEnum = platform.ToPlatform();
        if (platformEnum != Platforms.android && platformEnum != Platforms.windows) throw new Exception("当前只支持windows和android客户端");

        var config = new VersionConfig();
        configuration.GetSection("Version").Bind(config);
        var httpService = _serviceProvider.GetRequiredService<IHttpService>();
        var version = await httpService.GetAsync<string>(new HttpKeyValueRequest(config.GiteeVersionUrl));
        if (!version.IsSuccess) version = await httpService.GetAsync<string>(new HttpKeyValueRequest(config.GithubVersionUrl));
        if (!version.IsSuccess) return DataReply<VersionDto>.Failed(version.Message);
        return DataReply<VersionDto>.Succeed(new VersionDto(config, version.Data, platformEnum));
    }
}
