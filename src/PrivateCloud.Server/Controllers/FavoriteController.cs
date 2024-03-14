using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Model;
using SixLabors.ImageSharp;

namespace PrivateCloud.Server.Controllers;

public class FavoriteController(IServiceProvider serviceProvider, IRepository<FavoriteEntity> repository, IRepository<ThumbEntity> thumbRepository, IRepository<MediaLibEntity> mediaLibRepository) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageResult<FavoriteReply> Get([FromQuery] FavoriteQueryRequest request)
    {
        var query = repository.GetAll();
        if (request.Name.NotEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));
        var count = query.Count();
        var data = query.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();
        var result = _mapper.Map<List<FavoriteReply>>(data);
        SetExtraInfo(result);
        return Result.SucceedPage(result, count, request.PageIndex, request.PageSize);
    }

    void SetExtraInfo(List<FavoriteReply> entries)
    {
        if (entries.IsEmpty()) return;
        entries.ForEach(x =>
        {
            var model = new IdPath(x.IdPath);
            x.IsEncrypt = model.IsEncrypt;
            x.IsFolder = model.IsFolder;
            x.EncryptId = model.Name.ToGuid();
            x.MediaLibId = model.MediaLibId;
        });
        var idPathList = entries.Where(x => !x.IsEncrypt).Select(x => x.IdPath).ToList();
        var thumbs = thumbRepository.GetMany(x => idPathList.Contains(x.IdPath)).ToList();
        var mediaLibIds = entries.Select(x => x.MediaLibId).Distinct().ToList();
        var mediaLibs = mediaLibRepository.GetMany(x => mediaLibIds.Contains(x.Id)).ToList();
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
    public Result<string> Post(string idPath, [FromBody] FavoriteAddRequest request)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));

        var oldEntities = repository.GetMany(x => x.UserId == CurrentUser.Id && x.IdPath == idPath).ToList();
        repository.RemoveRange(oldEntities);

        var entity = new FavoriteEntity
        {
            IsFolder = request.IsFolder,
            MediaLibId = mediaLib.Id,
            Name = request.Name,
            IdPath = idPathModel.Value,
            UserId = CurrentUser.Id,
        };
        repository.Add(entity);
        return Result.Succeed<string>(entity.Id.ToString());
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
