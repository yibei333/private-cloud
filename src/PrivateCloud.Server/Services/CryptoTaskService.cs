using Hangfire;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace PrivateCloud.Server.Services;

public class CryptoTaskService
{
    private static readonly object _locker = new();
    private static bool _running = false;

    readonly FfmpegService ffmpegService;
    readonly ILogger<CryptoTaskService> logger;
    readonly DataContext dbContext;

    public CryptoTaskService(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.CreateScope().ServiceProvider;
        ffmpegService = provider.GetRequiredService<FfmpegService>();
        logger = provider.GetRequiredService<ILogger<CryptoTaskService>>();
        dbContext = provider.GetRequiredService<DataContext>();
    }

    public async Task ScanToProcessCryptoTask()
    {
        lock (_locker)
        {
            if (_running) return;
            _running = true;
        }

        var task = dbContext.CryptoTask.Where(x => x.HandleCount <= Statics.TaskMaxHandleCount).OrderBy(x => x.Id).FirstOrDefault();
        if (task is null)
        {
            _running = false;
            return;
        }

        await ProcessCryptoTask(task);
        BackgroundJob.Enqueue(() => ScanToAddCryptoTask(dbContext.MediaLib.FirstOrDefault(x => x.Id == task.MediaLibId), task.Type));
        _running = false;
    }

    public void ScanToAddCryptoTask(MediaLibEntity mediaLib, CryptoTaskType type)
    {
        if (dbContext.CryptoTask.Any(x => x.MediaLibId == mediaLib.Id && x.HandleCount <= Statics.TaskMaxHandleCount))
        {
            BackgroundJob.Enqueue(() => ScanToProcessCryptoTask());
            return;
        }
        var direcotry = new DirectoryInfo(mediaLib.Path);
        var tasks = new List<CryptoTaskEntity>();
        direcotry.GetDirectories().ToList().ForEach(x => tasks.AddRange(BuildFolderCryptoTask(mediaLib, type, x, 0)));
        direcotry.GetFiles().ToList().ForEach(x => tasks.Add(BuildFileCryptoTask(mediaLib, type, x)));

        var names = tasks.Select(x => x.FullName).ToList();
        if (type == CryptoTaskType.Encrypt)
        {
            var idConverters = tasks.Select(x => new { x.TaskId, NameId = Guid.TryParse(x.IsFolder ? new DirectoryInfo(x.FullName).Name : new FileInfo(x.FullName).Name, out var id) ? id : Guid.Empty }).ToList();
            var ids = idConverters.Where(x => x.NameId.NotEmpty()).Select(x => x.NameId).Distinct().ToList();
            if (ids.NotNullOrEmpty())
            {
                var exsitedFiles = dbContext.EncryptedFile.Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToList();
                var existedTasks = (from a in tasks join b in idConverters on a.TaskId equals b.TaskId join c in exsitedFiles on b.NameId equals c select a);
                if (existedTasks.Any()) tasks = tasks.Except(existedTasks).ToList();
            }
        }
        else
        {
            var idConverters = tasks.Select(x => new { x.TaskId, NameId = Guid.TryParse(x.IsFolder ? new DirectoryInfo(x.FullName).Name : new FileInfo(x.FullName).Name, out var id) ? id : Guid.Empty }).ToList();
            var ids = idConverters.Where(x => x.NameId.NotEmpty()).Select(x => x.NameId).Distinct().ToList();
            if (ids.IsNullOrEmpty()) tasks = [];
            else
            {
                var exsitedFiles = dbContext.EncryptedFile.Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToList();
                tasks = (from a in tasks join b in idConverters on a.TaskId equals b.TaskId join c in exsitedFiles on b.NameId equals c select a).ToList();
            }
        }
        dbContext.CryptoTask.AddRange(tasks.OrderBy(x => x.IsFolder).ThenByDescending(x => x.Deepth));

        var failedTasks = dbContext.CryptoTask.Where(x => x.MediaLibId == mediaLib.Id && x.HandleCount > Statics.TaskMaxHandleCount).ToList();
        dbContext.CryptoTask.RemoveRange(failedTasks);
        dbContext.SaveChanges();
        BackgroundJob.Enqueue(() => ScanToProcessCryptoTask());
    }

    static List<CryptoTaskEntity> BuildFolderCryptoTask(MediaLibEntity mediaLib, CryptoTaskType type, DirectoryInfo directory, int deepth)
    {
        var tasks = new List<CryptoTaskEntity> { new() { FullName = directory.FullName, IsFolder = true, MediaLibId = mediaLib.Id, Type = type, Deepth = deepth } };
        directory.GetDirectories().ToList().ForEach(x => tasks.AddRange(BuildFolderCryptoTask(mediaLib, type, x, ++deepth)));
        directory.GetFiles().ToList().ForEach(x => tasks.Add(BuildFileCryptoTask(mediaLib, type, x)));
        return tasks;
    }

    static CryptoTaskEntity BuildFileCryptoTask(MediaLibEntity mediaLib, CryptoTaskType type, FileInfo file) => new() { MediaLibId = mediaLib.Id, FullName = file.FullName, Type = type, IsFolder = false };

    async Task ProcessCryptoTask(CryptoTaskEntity task)
    {
        try
        {
            logger.LogInformation("开始处理任务【{taskId}】", task.Id);
            if (task.Type == CryptoTaskType.Encrypt) await ProcessEncryptTask(task);
            else if (task.Type == CryptoTaskType.Decrypt) await ProcessDecryptTask(task);

            dbContext.CryptoTask.Remove(task);
            dbContext.SaveChanges();
            logger.LogInformation("处理任务【{taskId}】成功", task.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理任务【{taskId}】失败:{message}", task.Id, ex.Message);
            task.HandleCount += 1;
            task.Message = ex.Message;
            dbContext.CryptoTask.Update(task);
            dbContext.SaveChanges();
        }
    }

    async Task ProcessEncryptTask(CryptoTaskEntity task)
    {
        if (task.IsFolder)
        {
            var directoryInfo = new DirectoryInfo(task.FullName);
            var folderId = Guid.TryParse(directoryInfo.Name, out var folderNameId) ? folderNameId : Guid.Empty;
            if (folderId.NotEmpty() && dbContext.EncryptedFile.Any(x => x.Id == folderId)) return;
            if (folderId.IsEmpty()) folderId = task.TaskId;
            var newName = directoryInfo.Parent.FullName.CombinePath(folderId.ToString());
            dbContext.EncryptedFile.Add(new EncryptedFileEntity { Id = folderId, Name = directoryInfo.Name, MediaLibId = task.MediaLibId });
            dbContext.SaveChanges();
            Directory.Move(task.FullName, newName);
            return;
        }

        var fileInfo = new FileInfo(task.FullName);
        var id = Guid.TryParse(fileInfo.Name, out var nameId) ? nameId : Guid.Empty;
        if (id.NotEmpty() && dbContext.EncryptedFile.Any(x => x.Id == id)) return;
        if (id.IsEmpty()) id = task.TaskId;

        //0.解析数据
        var idString = id.ToString();
        var key = Guid.NewGuid().ToString();
        var iv = CryptoExtension.GenerateAesIV();
        var ivBytes = Encoding.UTF8.GetBytes(iv);
        using var aes = Aes.Create();
        aes.SetKey(key.Utf8Decode());
        aes.SetIV(ivBytes);
        if (!fileInfo.Exists) throw new FileNotFoundException(null, task.FullName);
        var hasTempFolder = true;

        //1.创建临时文件夹
        var tempPath = Statics.TempPath.CombinePath(idString);
        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        Directory.CreateDirectory(tempPath);

        //2.处理缩略图
        if (task.FullName.IsVideo())
        {
            await ffmpegService.GetVideoThumbAsync(id, task.FullName);
            var thumbPath = tempPath.CombinePath($"{id}.png");
            var gifPath = tempPath.CombinePath($"{id}.gif");
            var gridImagePath = tempPath.CombinePath($"{id}.grid.png");
            using var thumbStream = new FileInfo(thumbPath).OpenOrCreate();
            using var gifStream = new FileInfo(gifPath).OpenOrCreate();
            using var gridImageStream = new FileInfo(gridImagePath).OpenOrCreate();
            using var encryptedThumbStream = new FileInfo(tempPath.CombinePath($"{id}.png.encrypted")).OpenOrCreate();
            using var encryptedGifStream = new FileInfo(tempPath.CombinePath($"{id}.gif.encrypted")).OpenOrCreate();
            using var encryptedGridImageStream = new FileInfo(tempPath.CombinePath($"{id}.grid.png.encrypted")).OpenOrCreate();

            aes.Encrypt(thumbStream, encryptedThumbStream);
            aes.Encrypt(gifStream, encryptedGifStream);
            aes.Encrypt(gridImageStream, encryptedGridImageStream);
            thumbStream.Dispose();
            gifStream.Dispose();
            gridImageStream.Dispose();

            File.Delete(thumbPath);
            File.Delete(gifPath);
            File.Delete(gridImagePath);
        }
        else if (task.FullName.IsGif())
        {
            await ffmpegService.GetGifThumbAsync(id, task.FullName);
            var thumbPath = tempPath.CombinePath($"{id}.png");
            using var thumbStream = new FileInfo(thumbPath).OpenOrCreate();
            using var encryptedThumbStream = new FileInfo(tempPath.CombinePath($"{id}.png.encrypted")).OpenOrCreate();
            aes.Encrypt(thumbStream, encryptedThumbStream);
            thumbStream.Dispose();
            File.Delete(thumbPath);
        }
        else if (task.FullName.IsImage())
        {
            await ffmpegService.GetImageThumbAsync(id, task.FullName);
            var thumbPath = tempPath.CombinePath($"{id}.png");
            using var thumbStream = new FileInfo(thumbPath).OpenOrCreate();
            using var encryptedThumbStream = new FileInfo(tempPath.CombinePath($"{id}.png.encrypted")).OpenOrCreate();
            aes.Encrypt(thumbStream, encryptedThumbStream);
            thumbStream.Dispose();
            File.Delete(thumbPath);
        }
        else
        {
            hasTempFolder = false;
        }

        //3.处理HLS
        if (task.FullName.IsVideo())
        {
            var keyPath = tempPath.CombinePath($"{id}.key");
            var keyInfoPath = tempPath.CombinePath("keyinfo.txt");
            var builder = new StringBuilder();
            builder.AppendLine($"/api/file/m3u8/key/{id}.key");
            builder.AppendLine(keyPath);
            builder.AppendLine(ivBytes.HexStringEncode());
            File.WriteAllText(keyInfoPath, builder.ToString());
            File.WriteAllText(keyPath, key);
            var hlsPath = tempPath.CombinePath($"{id}.m3u8");
            var hlsPartsFolder = tempPath.CombinePath("ts");
            var hlsPartsBaseUrl = $"ts/{id}/";
            var hlsPartsPath = hlsPartsFolder.CombinePath($"%d.ts");
            await ffmpegService.ConvertVideoToHlsAsync(id, task.FullName, hlsPath, hlsPartsPath, hlsPartsBaseUrl, keyInfoPath);
        }

        //4.加密文件
        var targetPath = tempPath.CombinePath(idString);
        using var sourceStream = new FileInfo(task.FullName).OpenOrCreate();
        using var targetStream = new FileInfo(targetPath).OpenOrCreate();
        aes.Encrypt(sourceStream, targetStream);
        sourceStream.Dispose();
        targetStream.Dispose();
        File.Move(targetPath, fileInfo.Directory.FullName.CombinePath(idString), true);
        dbContext.EncryptedFile.Add(new EncryptedFileEntity { Id = id, IV = iv, Key = key, Name = fileInfo.Name, MediaLibId = task.MediaLibId });
        dbContext.SaveChanges();

        //5.清理
        fileInfo.Delete();
        if (!hasTempFolder && Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
    }

    async Task ProcessDecryptTask(CryptoTaskEntity task)
    {
        if (task.IsFolder)
        {
            var directoryInfo = new DirectoryInfo(task.FullName);
            var folderId = Guid.TryParse(directoryInfo.Name, out var folderNameId) ? folderNameId : Guid.Empty;
            if (folderId.IsEmpty()) return;
            var encryptedFolder = dbContext.EncryptedFile.FirstOrDefault(x => x.Id == folderId);
            if (encryptedFolder is null) return;
            var newPath = directoryInfo.Parent.FullName.CombinePath(encryptedFolder.Name);
            Directory.Move(task.FullName, newPath);
            dbContext.EncryptedFile.Remove(encryptedFolder);
            dbContext.SaveChanges();
            return;
        }

        var fileInfo = new FileInfo(task.FullName);
        var id = Guid.TryParse(fileInfo.Name, out var nameId) ? nameId : Guid.Empty;
        if (id.IsEmpty() || !dbContext.EncryptedFile.Any(x => x.Id == id)) return;

        var encryptedFile = dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id);
        if (encryptedFile is null) return;
        using var aes = Aes.Create();
        aes.SetKey(encryptedFile.Key.Utf8Decode());
        aes.SetIV(encryptedFile.IV.Utf8Decode());

        //0.解析数据
        if (!fileInfo.Exists) throw new FileNotFoundException(null, task.FullName);
        var targetPath = fileInfo.Directory.FullName.CombinePath(encryptedFile.Name);

        //1.解密文件
        using var sourceStream = new FileInfo(task.FullName).OpenOrCreate();
        using var targetStream = new FileInfo(targetPath).OpenOrCreate();
        aes.Decrypt(sourceStream, targetStream);
        sourceStream.Dispose();
        targetStream.Dispose();

        //2.清理
        dbContext.EncryptedFile.Remove(encryptedFile);
        dbContext.SaveChanges();
        var tempPath = Statics.TempPath.CombinePath(encryptedFile.Id.ToString());
        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        File.Delete(task.FullName);
        await Task.CompletedTask;
    }
}