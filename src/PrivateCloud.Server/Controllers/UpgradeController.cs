using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

[Authorize(Roles = StaticNames.AdminName)]
public class UpgradeController(IServiceProvider serviceProvider, IRepository<UpgradeEntity> repository) : BaseController(serviceProvider)
{
    [HttpGet]
    [Route("options")]
    public Result<List<OptionsModel>> GetOptions()
    {
        var result = new List<OptionsModel>
        {
            new("Platforms",EnumUtil.GetKeyValues<Platforms>().Select(x=>new KeyValueModel(x.Key,x.Value)).ToList())
        };
        return Result.Succeed(result);
    }

    [HttpGet]
    [Route("last/{platform}")]
    public Result<UpgradeReply> GetLast(string platform)
    {
        var platformEnum = platform.ToPlatform();
        var entity = repository.GetMany(x => x.Platform == platformEnum).OrderByDescending(x => x.CreateTime).FirstOrDefault();
        if (entity is null) return Result.Succeed<UpgradeReply>(null);
        var result = _mapper.Map<UpgradeReply>(entity);
        return Result.Succeed(result);
    }

    [HttpGet]
    public PageResult<UpgradeReply> Get([FromQuery] UpgradeQueryRequest request)
    {
        var query = repository.GetAll();
        if (request.Version.NotEmpty()) query = query.Where(x => x.Version.ToLower().Contains(request.Version.ToLower()));
        var count = query.Count();
        var data = query.OrderByDescending(x => x.CreateTime).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();
        var result = _mapper.Map<List<UpgradeReply>>(data);
        return Result.SucceedPage(result, count, request.PageIndex, request.PageSize);
    }

    [HttpGet("{id}")]
    public Result<UpgradeReply> Get(Guid id)
    {
        var entity = repository.Get(x => x.Id == id) ?? throw new DataNotFoundException();
        var result = _mapper.Map<UpgradeReply>(entity);
        return Result.Succeed(result);
    }

    [HttpPost]
    public Result Post([FromBody] UpgradeAddRequest request)
    {
        if (request.Version.IsEmpty()) throw new ParameterRequiredException(nameof(request.Version));
        if (request.Url.IsEmpty()) throw new ParameterRequiredException(nameof(request.Url));
        if (request.LocalUrl.IsEmpty()) throw new ParameterRequiredException(nameof(request.LocalUrl));
        if (!Enum.IsDefined(request.Platform)) throw new PlatformErrorException();
        if (repository.Any(x => x.Version == request.Version && x.Platform == request.Platform)) throw new PlatformVersionExistException();

        var entity = new UpgradeEntity { Version = request.Version, Url = request.Url, LocalUrl = request.LocalUrl, Platform = request.Platform };
        repository.Add(entity);
        return Result.Succeed();
    }

    [HttpPut("{id}")]
    public Result Put(Guid id, [FromBody] UpgradeModifyRequest request)
    {
        if (request.Version.IsEmpty()) throw new ParameterRequiredException(nameof(request.Version));
        if (request.Url.IsEmpty()) throw new ParameterRequiredException(nameof(request.Url));
        if (request.LocalUrl.IsEmpty()) throw new ParameterRequiredException(nameof(request.LocalUrl));
        if (!Enum.IsDefined(request.Platform)) throw new PlatformErrorException();
        if (repository.Any(x => x.Version == request.Version && x.Platform == request.Platform && x.Id != id)) throw new PlatformVersionExistException();

        var entity = repository.Get(x => x.Id == id) ?? throw new DataNotFoundException();
        entity.Version = request.Version;
        entity.Url = request.Url;
        entity.LocalUrl = request.LocalUrl;
        entity.Platform = request.Platform;
        repository.Update(entity);
        return Result.Succeed();
    }

    [HttpDelete("{id}")]
    public Result Delete(Guid id)
    {
        var entity = repository.Get(x => x.Id == id) ?? throw new DataNotFoundException();
        repository.Remove(entity);
        return Result.Succeed();
    }
}
