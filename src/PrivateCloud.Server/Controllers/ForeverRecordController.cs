using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;

namespace PrivateCloud.Server.Controllers;

public class ForeverRecordController(IServiceProvider serviceProvider) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageReply<ForeverRecordDto> Get([FromQuery] ForeverRecordRequest request)
    {
        var records = _dbContext.ForeverRecord.ToList();
        var query = _mapper.Map<List<ForeverRecordDto>>(records);
        if (request.Name.NotNullOrEmpty()) query = query.Where(x => x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        var count = query.Count;
        var data = query.OrderByDescending(x => x.CreateTime).Skip(request.Index * request.Size).Take(request.Size).ToList();
        return PageReply<ForeverRecordDto>.Succeed(data, count, request);
    }

    [HttpGet("{idPath}")]
    public DataReply<string> Get(string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        if (idPathModel.IsEncrypt) throw new EncryptFileNotSupportException();

        var entity = _dbContext.ForeverRecord.FirstOrDefault(x => x.IdPath == idPathModel.Value);
        if (entity is null)
        {
            var salt = Guid.NewGuid();
            var sinature = $"{mediaLib.Id}{idPathModel.RelativePath}{salt}".Utf8Decode().Sha256();
            entity = new ForeverRecordEntity
            {
                MediaLibId = mediaLib.Id,
                IdPath = idPathModel.Value,
                UserId = CurrentUser.Id,
                Salt = salt,
                Signature = sinature
            };
            _dbContext.ForeverRecord.Add(entity);
            _dbContext.SaveChanges();
        }
        return DataReply<string>.Succeed(entity.Signature);
    }

    [HttpGet]
    [Route("file/{signature}")]
    [AllowAnonymous]
    public FileResult GetFile(string signature)
    {
        var entity = _dbContext.ForeverRecord.FirstOrDefault(x => x.Signature == signature) ?? throw new DataNotFoundException();
        var idPathModel = new IdPath(entity.IdPath);
        if (idPathModel.IsEncrypt) throw new NotSupportedException();
        var fileInfo = new FileInfo(idPathModel.AbsolutePath);
        if (!fileInfo.Exists) throw new FileNotFoundException();
        return BuildFileStreamResult(idPathModel.Name, fileInfo.OpenRead());
    }

    [HttpDelete("{id}")]
    public EmptyReply Delete(Guid id)
    {
        var entity = _dbContext.ForeverRecord.FirstOrDefault(x => x.Id == id);
        if (entity is null) return EmptyReply.Succeed();
        _dbContext.ForeverRecord.Remove(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }
}
