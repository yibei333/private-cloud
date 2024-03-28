using Hangfire;
using Microsoft.EntityFrameworkCore;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Extensions.Data;

namespace PrivateCloud.Server.Services;

public class ThumbTaskService
{
    private static readonly object _locker = new();
    private static bool _isRunning;
    private static readonly int MaxHandleCount = 5;
    readonly FfmpegService ffmpegService;
    readonly ILogger<ThumbTaskService> logger;
    readonly IRepository<MediaLibEntity> mediaLibRepository;
    readonly IRepository<ThumbEntity> thumbRepository;
    readonly IRepository<ThumbTaskEntity> thumbTaskRepository;

    public ThumbTaskService(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.CreateScope().ServiceProvider;
        ffmpegService = provider.GetRequiredService<FfmpegService>();
        logger = provider.GetRequiredService<ILogger<ThumbTaskService>>();
        mediaLibRepository = provider.GetRequiredService<IRepository<MediaLibEntity>>();
        thumbRepository = provider.GetRequiredService<IRepository<ThumbEntity>>();
        thumbTaskRepository = provider.GetRequiredService<IRepository<ThumbTaskEntity>>();
    }

    public async Task ScanToProcessThumbTaskAsync()
    {
        lock (_locker)
        {
            if (_isRunning) return;
            _isRunning = true;
        }
        var ffmpegPath = Statics.GetFfmpegPath();
        if (ffmpegPath.IsNull())
        {
            logger.LogInformation("找不到文件:{ffmpegPath},缩略图服务不能正常工作", ffmpegPath);
            _isRunning = false;
            return;
        }
        var tasks = await thumbTaskRepository.GetMany(x => x.HandledCount <= MaxHandleCount).OrderBy(x => x.CreateTime).ToListAsync();
        foreach (var task in tasks)
        {
            logger.LogInformation("start process thumb:{taskId}", task.Id);
            try
            {
                await ProcessAsync(task.IdPath);
                logger.LogInformation("process thumb complete:{taskId}", task.Id);
                thumbTaskRepository.Remove(task);
            }
            catch (Exception ex)
            {
                task.HandledCount += 1;
                thumbTaskRepository.Update(task);
                logger.LogError(ex, "process thumb failed:{taskId}", task.Id);
            }
        }
        _isRunning = false;
        if (tasks.Count != 0) BackgroundJob.Enqueue(() => ScanToProcessThumbTaskAsync());
    }

    public async void ScanTaskToWriteAsync(string idPathValue)
    {
        var idPathModel = new IdPath(idPathValue);
        var mediaLib = mediaLibRepository.Get(x => x.Id == idPathModel.MediaLibId) ?? throw new MediaLibNotFoundException();
        var files = new DirectoryInfo(idPathModel.AbsolutePath).GetFiles().ToList().Where(x => x.Name.IsVideo() || x.Name.IsGif() || x.Name.IsImage()).Select(x => new
        {
            IdPath = new IdPath(mediaLib, x.FullName, false).Value,
            LastModifyTime = x.LastWriteTime
        }).ToList();
        var paths = files.Select(x => x.IdPath).ToList();
        var existedThumbs = thumbRepository.GetMany(x => paths.Contains(x.IdPath)).Select(x => x.IdPath).ToList();
        var existedTasks = await thumbTaskRepository.GetMany(x => x.HandledCount <= MaxHandleCount && paths.Contains(x.IdPath)).Select(x => x.IdPath).ToListAsync();
        var notExists = paths.Except(existedThumbs).Except(existedTasks).ToList();
        if (notExists.Count != 0)
        {
            var tasks = notExists.Select(x => new ThumbTaskEntity { IdPath = x });
            thumbTaskRepository.AddRange(tasks);
            BackgroundJob.Enqueue(() => ScanToProcessThumbTaskAsync());
        }
    }

    private async Task ProcessAsync(string idPathValue)
    {
        Statics.TempPath.EnsureDirectoryExist();
        var idPathModel = new IdPath(idPathValue);
        var mediaLib = mediaLibRepository.Get(x => x.Id == idPathModel.MediaLibId) ?? throw new MediaLibNotFoundException();
        if (idPathModel.AbsolutePath.IsVideo()) await HanldeVideo(mediaLib, idPathModel);
        else if (idPathModel.AbsolutePath.IsGif()) await HanldeGif(mediaLib, idPathModel);
        else if (idPathModel.AbsolutePath.IsImage()) await HanldeImage(mediaLib, idPathModel);
    }

    private async Task HanldeVideo(MediaLibEntity mediaLib, IdPath idPath)
    {
        var thumb = thumbRepository.Get(x => x.IdPath == idPath.Value);
        var fileInfo = new FileInfo(idPath.AbsolutePath);
        if (thumb is null)
        {
            thumb = new ThumbEntity { FileLastModifyTime = fileInfo.LastWriteTime, MediaLibId = mediaLib.Id, IdPath = idPath.Value };
            await ffmpegService.GetVideoThumbAsync(thumb.Id, idPath.AbsolutePath);
            thumbRepository.Add(thumb);
        }
        else
        {
            if (thumb.FileLastModifyTime != fileInfo.LastWriteTime)
            {
                thumb.FileLastModifyTime = fileInfo.LastWriteTime;
                await ffmpegService.GetVideoThumbAsync(thumb.Id, idPath.AbsolutePath);
                thumbRepository.Update(thumb);
            }
        }
    }

    private async Task HanldeGif(MediaLibEntity mediaLib, IdPath idPath)
    {
        var thumb = thumbRepository.Get(x => x.IdPath == idPath.Value);
        var fileInfo = new FileInfo(idPath.AbsolutePath);
        if (thumb is null)
        {
            thumb = new ThumbEntity { FileLastModifyTime = fileInfo.LastWriteTime, MediaLibId = mediaLib.Id, IdPath = idPath.Value };
            await ffmpegService.GetGifThumbAsync(thumb.Id, idPath.AbsolutePath);
            thumbRepository.Add(thumb);
        }
        else
        {
            if (thumb.FileLastModifyTime != fileInfo.LastWriteTime)
            {
                thumb.FileLastModifyTime = fileInfo.LastWriteTime;
                await ffmpegService.GetGifThumbAsync(thumb.Id, idPath.AbsolutePath);
                thumbRepository.Update(thumb);
            }
        }
    }

    private async Task HanldeImage(MediaLibEntity mediaLib, IdPath idPath)
    {
        var thumb = thumbRepository.Get(x => x.IdPath == idPath.Value);
        var fileInfo = new FileInfo(idPath.AbsolutePath);
        if (thumb is null)
        {
            thumb = new ThumbEntity { FileLastModifyTime = fileInfo.LastWriteTime, MediaLibId = mediaLib.Id, IdPath = idPath.Value };
            await ffmpegService.GetImageThumbAsync(thumb.Id, idPath.AbsolutePath);
            thumbRepository.Add(thumb);
        }
        else
        {
            if (thumb.FileLastModifyTime != fileInfo.LastWriteTime)
            {
                thumb.FileLastModifyTime = fileInfo.LastWriteTime;
                await ffmpegService.GetImageThumbAsync(thumb.Id, idPath.AbsolutePath);
                thumbRepository.Update(thumb);
            }
        }
    }
}