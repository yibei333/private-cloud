using Hangfire;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Models;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Encryption;
using SixLabors.ImageSharp;
using System.Text;

namespace PrivateCloud.Server.Services;

public class CryptoTaskService
{
    private static readonly object _locker = new();
    private static bool _running = false;

    readonly FfmpegService ffmpegService;
    readonly IEncryption encryption;
    readonly ILogger<CryptoTaskService> logger;
    readonly IRepository<EncryptedFileEntity> encryptedFileRepository;
    readonly IRepository<MediaLibEntity> mediaLibRepository;
    readonly IRepository<CryptoTaskEntity> cryptoTaskRepository;

    public CryptoTaskService(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.CreateScope().ServiceProvider;
        ffmpegService = provider.GetRequiredService<FfmpegService>();
        encryption = provider.GetRequiredService<IEncryption>();
        logger = provider.GetRequiredService<ILogger<CryptoTaskService>>();
        encryptedFileRepository = provider.GetRequiredService<IRepository<EncryptedFileEntity>>();
        cryptoTaskRepository = provider.GetRequiredService<IRepository<CryptoTaskEntity>>();
        mediaLibRepository = provider.GetRequiredService<IRepository<MediaLibEntity>>();
    }

    public async Task ScanToProcessCryptoTask()
    {
        lock (_locker)
        {
            if (_running) return;
            _running = true;
        }

        var task = cryptoTaskRepository.GetMany(x => x.HandleCount <= Statics.TaskMaxHandleCount).OrderBy(x => x.Id).FirstOrDefault();
        if (task is null)
        {
            _running = false;
            return;
        }

        await ProcessCryptoTask(task);
        BackgroundJob.Enqueue(() => ScanToAddCryptoTask(mediaLibRepository.Get(x => x.Id == task.MediaLibId), task.Type));
        _running = false;
    }

    public void ScanToAddCryptoTask(MediaLibEntity mediaLib, CryptoTaskType type)
    {
        if (cryptoTaskRepository.Any(x => x.MediaLibId == mediaLib.Id && x.HandleCount <= Statics.TaskMaxHandleCount))
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
            if (ids.NotEmpty())
            {
                var exsitedFiles = encryptedFileRepository.GetMany(x => ids.Contains(x.Id)).Select(x => x.Id).ToList();
                var existedTasks = (from a in tasks join b in idConverters on a.TaskId equals b.TaskId join c in exsitedFiles on b.NameId equals c select a);
                if (existedTasks.Any()) tasks = tasks.Except(existedTasks).ToList();
            }
        }
        else
        {
            var idConverters = tasks.Select(x => new { x.TaskId, NameId = Guid.TryParse(x.IsFolder ? new DirectoryInfo(x.FullName).Name : new FileInfo(x.FullName).Name, out var id) ? id : Guid.Empty }).ToList();
            var ids = idConverters.Where(x => x.NameId.NotEmpty()).Select(x => x.NameId).Distinct().ToList();
            if (ids.IsEmpty()) tasks = [];
            else
            {
                var exsitedFiles = encryptedFileRepository.GetMany(x => ids.Contains(x.Id)).Select(x => x.Id).ToList();
                tasks = (from a in tasks join b in idConverters on a.TaskId equals b.TaskId join c in exsitedFiles on b.NameId equals c select a).ToList();
            }
        }
        cryptoTaskRepository.AddRange(tasks.OrderBy(x => x.IsFolder).ThenByDescending(x => x.Deepth));

        var failedTasks = cryptoTaskRepository.GetMany(x => x.MediaLibId == mediaLib.Id && x.HandleCount > Statics.TaskMaxHandleCount).ToList();
        cryptoTaskRepository.RemoveRange(failedTasks);
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

            cryptoTaskRepository.Remove(task);
            logger.LogInformation("处理任务【{taskId}】成功", task.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理任务【{taskId}】失败:{message}", task.Id, ex.Message);
            task.HandleCount += 1;
            task.Message = ex.Message;
            cryptoTaskRepository.Update(task);
        }
    }

    async Task ProcessEncryptTask(CryptoTaskEntity task)
    {
        if (task.IsFolder)
        {
            var directoryInfo = new DirectoryInfo(task.FullName);
            var folderId = Guid.TryParse(directoryInfo.Name, out var folderNameId) ? folderNameId : Guid.Empty;
            if (folderId.NotEmpty() && encryptedFileRepository.Any(x => x.Id == folderId)) return;
            if (folderId.IsEmpty()) folderId = task.TaskId;
            var newName = directoryInfo.Parent.FullName.CombinePath(folderId.ToString());
            encryptedFileRepository.Add(new EncryptedFileEntity { Id = folderId, Name = directoryInfo.Name, MediaLibId = task.MediaLibId });
            Directory.Move(task.FullName, newName);
            return;
        }

        var fileInfo = new FileInfo(task.FullName);
        var id = Guid.TryParse(fileInfo.Name, out var nameId) ? nameId : Guid.Empty;
        if (id.NotEmpty() && encryptedFileRepository.Any(x => x.Id == id)) return;
        if (id.IsEmpty()) id = task.TaskId;

        //0.解析数据
        var idString = id.ToString();
        var key = Guid.NewGuid().ToString();
        var iv = CryptoExtension.GenerateAesIV();
        var ivBytes = Encoding.UTF8.GetBytes(iv);
        var encryptionOption = new AesEncryptOption(key, ivBytes);
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
            var encryptedThumbPath = tempPath.CombinePath($"{id}.png.encrypted");
            var encryptedGifPath = tempPath.CombinePath($"{id}.gif.encrypted");
            var encryptedGridImagePath = tempPath.CombinePath($"{id}.grid.png.encrypted");
            encryption.Symmetric.Aes.EncryptFile(thumbPath, encryptedThumbPath, encryptionOption);
            encryption.Symmetric.Aes.EncryptFile(gifPath, encryptedGifPath, encryptionOption);
            encryption.Symmetric.Aes.EncryptFile(gridImagePath, encryptedGridImagePath, encryptionOption);
            File.Delete(thumbPath);
            File.Delete(gifPath);
            File.Delete(gridImagePath);
        }
        else if (task.FullName.IsGif())
        {
            await ffmpegService.GetGifThumbAsync(id, task.FullName);
            var thumbPath = tempPath.CombinePath($"{id}.png");
            var encryptedThumbPath = tempPath.CombinePath($"{id}.png.encrypted");
            encryption.Symmetric.Aes.EncryptFile(thumbPath, encryptedThumbPath, encryptionOption);
            File.Delete(thumbPath);
        }
        else if (task.FullName.IsImage())
        {
            await ffmpegService.GetImageThumbAsync(id, task.FullName);
            var thumbPath = tempPath.CombinePath($"{id}.png");
            var encryptedThumbPath = tempPath.CombinePath($"{id}.png.encrypted");
            encryption.Symmetric.Aes.EncryptFile(thumbPath, encryptedThumbPath, encryptionOption);
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
            builder.AppendLine(ivBytes.ToHexString());
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
        encryption.Symmetric.Aes.EncryptFile(task.FullName, targetPath, encryptionOption);
        File.Move(targetPath, fileInfo.Directory.FullName.CombinePath(idString), true);
        encryptedFileRepository.Add(new EncryptedFileEntity { Id = id, IV = iv, Key = key, Name = fileInfo.Name, MediaLibId = task.MediaLibId });

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
            var encryptedFolder = encryptedFileRepository.Get(x => x.Id == folderId);
            if (encryptedFolder is null) return;
            var newPath = directoryInfo.Parent.FullName.CombinePath(encryptedFolder.Name);
            Directory.Move(task.FullName, newPath);
            encryptedFileRepository.Remove(encryptedFolder);
            return;
        }

        var fileInfo = new FileInfo(task.FullName);
        var id = Guid.TryParse(fileInfo.Name, out var nameId) ? nameId : Guid.Empty;
        if (id.IsEmpty() || !encryptedFileRepository.Any(x => x.Id == id)) return;

        var encryptedFile = encryptedFileRepository.Get(x => x.Id == id);
        if (encryptedFile is null) return;

        //0.解析数据
        var decryptionOption = new AesDecryptOption(encryptedFile.Key, Encoding.UTF8.GetBytes(encryptedFile.IV));
        if (!fileInfo.Exists) throw new FileNotFoundException(null, task.FullName);
        var targetPath = fileInfo.Directory.FullName.CombinePath(encryptedFile.Name);

        //1.解密文件
        encryption.Symmetric.Aes.DecryptFile(task.FullName, targetPath, decryptionOption);

        //2.清理
        encryptedFileRepository.Remove(encryptedFile);
        var tempPath = Statics.TempPath.CombinePath(encryptedFile.Id.ToString());
        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        File.Delete(task.FullName);
        await Task.CompletedTask;
    }
}