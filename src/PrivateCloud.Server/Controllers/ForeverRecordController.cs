using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

public class ForeverRecordController(IServiceProvider serviceProvider, IRepository<ForeverRecordEntity> repository) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageResult<ForeverRecordReply> Get([FromQuery] ForeverRecordRequest request)
    {
        var records = repository.GetAll().ToList();
        var query = _mapper.Map<List<ForeverRecordReply>>(records);
        if (request.Name.NotEmpty()) query = query.Where(x => x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        var count = query.Count;
        var data = query.OrderByDescending(x => x.CreateTime).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();
        return Result.SucceedPage(data, count, request.PageIndex, request.PageSize);
    }

    [HttpGet("{idPath}")]
    public Result<string> Get(string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        if (idPathModel.IsEncrypt) throw new EncryptFileNotSupportException();

        var entity = repository.Get(x => x.IdPath == idPathModel.Value);
        if (entity is null)
        {
            var salt = Guid.NewGuid();
            var sinature = $"{mediaLib.Id}{idPathModel.RelativePath}{salt}".SHA256Hash();
            entity = new ForeverRecordEntity
            {
                MediaLibId = mediaLib.Id,
                IdPath = idPathModel.Value,
                UserId = CurrentUser.Id,
                Salt = salt,
                Signature = sinature
            };
            repository.Add(entity);
        }
        return Result.Succeed<string>(entity.Signature);
    }

    [HttpGet]
    [Route("file/{signature}")]
    [AllowAnonymous]
    public FileResult GetFile(string signature)
    {
        var entity = repository.Get(x => x.Signature == signature) ?? throw new DataNotFoundException();
        var idPathModel = new IdPath(entity.IdPath);
        if (idPathModel.IsEncrypt) throw new NotSupportedException();
        var fileInfo = new FileInfo(idPathModel.AbsolutePath);
        if (!fileInfo.Exists) throw new FileNotFoundException();
        return BuildFileStreamResult(idPathModel.Name, fileInfo.OpenRead());
    }

    [HttpDelete("{id}")]
    public Result Delete(Guid id)
    {
        var entity = repository.Get(x => x.Id == id);
        if (entity is null) return Result.Succeed();
        repository.Remove(entity);
        return Result.Succeed();
    }
}
