using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;

namespace PrivateCloud.Server.Controllers;

public class HistoryController(IServiceProvider serviceProvider) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageReply<HistoryDto> Get([FromQuery] HistoryQueryRequest request)
    {
        var query = _dbContext.History.Where(x => x.UserId == CurrentUser.Id);
        if (request.Name.NotNullOrEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));
        if (request.StartTimeLong.HasValue) query = query.Where(x => x.CreateTime >= request.StartTimeLong);
        if (request.EndTimeLong.HasValue) query = query.Where(x => x.CreateTime < request.EndTimeLong);

        var count = query.Count();
        var data = query.OrderByDescending(x => x.CreateTime).Skip(request.Index * request.Size).Take(request.Size).ToList();
        var result = _mapper.Map<List<HistoryDto>>(data);
        SetExtraInfo(result);
        return PageReply<HistoryDto>.Succeed(result, count, request);
    }

    void SetExtraInfo(List<HistoryDto> entries)
    {
        if (entries.IsNullOrEmpty()) return;
        entries.ForEach(x =>
        {
            var model = new IdPath(x.IdPath);
            x.IsEncrypt = model.IsEncrypt;
            x.IsFolder = model.IsFolder;
            x.EncryptId = model.Name.ToGuid();
            x.MediaLibId = model.MediaLibId;
        });
        var idPathList = entries.Where(x => !x.IsEncrypt).Select(x => x.IdPath).ToList();
        var thumbs = _dbContext.Thumb.Where(x => idPathList.Contains(x.IdPath)).ToList();
        var mediaLibIds = entries.Select(x => x.MediaLibId).Distinct().ToList();
        var mediaLibs = _dbContext.MediaLib.Where(x => mediaLibIds.Contains(x.Id)).ToList();
        entries.ForEach(entry =>
        {
            entry.MediaLibName = mediaLibs.FirstOrDefault(x => x.Id == entry.MediaLibId)?.Name;
            if (entry.Name.IsImage() || entry.Name.IsVideo() || entry.Name.IsGif())
            {
                var extension = entry.IsEncrypt ? ".encrypted" : "";
                var id = entry.EncryptId;
                if (!entry.IsEncrypt) id = thumbs.FirstOrDefault(x => x.IdPath == entry.IdPath)?.Id ?? Guid.Empty;
                if (id.NotEmpty())
                {
                    var thumbPath = Statics.TempPath.CombinePath($"{id}/{id}.png{extension}");
                    entry.HasThumb = new FileInfo(thumbPath).Exists;
                    if (entry.Name.IsVideo())
                    {
                        var largeThumbPath = VideoThumbIsGridImage ? Statics.TempPath.CombinePath($"{id}/{id}.grid.png{extension}") : Statics.TempPath.CombinePath($"{id}/{id}.gif{extension}");
                        entry.HasLargeThumb = new FileInfo(largeThumbPath).Exists;
                    }
                }
            }
        });
    }

    [HttpPost("{idPath}")]
    public EmptyReply Post(string idPath, [FromBody] HistoryAddRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);

        var oldEntities = _dbContext.History.Where(x => x.IdPath == idPath).ToList();
        _dbContext.History.RemoveRange(oldEntities);

        var entity = new HistoryEntity
        {
            MediaLibId = mediaLib.Id,
            Name = request.Name,
            IdPath = idPathModel.Value,
            UserId = CurrentUser.Id,
            Position = request.Position,
        };
        _dbContext.History.Add(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [HttpDelete("{id}")]
    public EmptyReply Delete(Guid id)
    {
        var entity = _dbContext.History.FirstOrDefault(x => x.Id == id);
        if (entity is null) return EmptyReply.Succeed();
        _dbContext.History.Remove(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [HttpDelete]
    [Route("clear")]
    public EmptyReply Clear()
    {
        var entityList = _dbContext.History.Where(x => x.UserId == CurrentUser.Id);
        if (entityList.IsNullOrEmpty()) return EmptyReply.Succeed();
        _dbContext.History.RemoveRange(entityList);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }
}
