using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;

namespace PrivateCloud.Server.Controllers;

public class FavoriteController(IServiceProvider serviceProvider) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageReply<FavoriteDto> Get([FromQuery] FavoriteQueryRequest request)
    {
        var query = _dbContext.Favorite.AsQueryable();
        if (request.Name.NotNullOrEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));
        var count = query.Count();
        var data = query.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name).Skip(request.Index * request.Size).Take(request.Size).ToList();
        var result = _mapper.Map<List<FavoriteDto>>(data);
        SetExtraInfo(result);
        return PageReply<FavoriteDto>.Succeed(result, count, request);
    }

    void SetExtraInfo(List<FavoriteDto> entries)
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
    public DataReply<string> Post(string idPath, [FromBody] FavoriteAddRequest request)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));

        var oldEntities = _dbContext.Favorite.Where(x => x.UserId == CurrentUser.Id && x.IdPath == idPath).ToList();
        _dbContext.Favorite.RemoveRange(oldEntities);

        var entity = new FavoriteEntity
        {
            IsFolder = request.IsFolder,
            MediaLibId = mediaLib.Id,
            Name = request.Name,
            IdPath = idPathModel.Value,
            UserId = CurrentUser.Id,
        };
        _dbContext.Favorite.Add(entity);
        _dbContext.SaveChanges();
        return DataReply<string>.Succeed(entity.Id.ToString());
    }

    [HttpDelete("{id}")]
    public EmptyReply Delete(Guid id)
    {
        var entity = _dbContext.Favorite.FirstOrDefault(x => x.Id == id);
        if (entity is null) return EmptyReply.Succeed();
        _dbContext.Favorite.Remove(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }
}
