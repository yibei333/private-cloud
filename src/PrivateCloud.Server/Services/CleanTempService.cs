using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;

namespace PrivateCloud.Server.Services;

public class CleanTempService
{
    readonly CryptoTaskService cryptoTaskService;
    readonly DataContext dbContext;

    public CleanTempService(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.CreateScope().ServiceProvider;
        cryptoTaskService = provider.GetRequiredService<CryptoTaskService>();
        dbContext = provider.GetRequiredService<DataContext>();
    }

    public async void CleanTemp()
    {
        await Task.Factory.StartNew(async () =>
        {
            var thumbList = dbContext.Thumb.ToList();
            var encryptedFileList = dbContext.EncryptedFile.ToList();
            var directory = new DirectoryInfo(Statics.TempPath);
            var directories = directory.GetDirectories().ToList();

            var dbIds = thumbList.Select(x => x.Id).Union(encryptedFileList.Select(x => x.Id)).ToList();
            var directoryIds = directories.Select(x => x.Name.ToGuid()).ToList();
            var sameIds = (from a in dbIds join b in directoryIds on a equals b select a).ToList();

            //clean directory
            directories.Except(directories.Where(x => sameIds.Contains(x.Name.ToGuid()))).Where(x => x.Exists).ToList().ForEach(x => x.Delete(true));

            //clean thumb table
            dbContext.Thumb.RemoveRange(thumbList.Except(thumbList.Where(x => sameIds.Contains(x.Id))));

            //decrypt or remove encrypted file
            var encryptedHandles = encryptedFileList.Except(encryptedFileList.Where(x => sameIds.Contains(x.Id))).ToList();
            if (encryptedHandles.Count != 0)
            {
                var mediaLibs = dbContext.MediaLib.Where(x => x.IsEncrypt).ToList();
                encryptedHandles.ForEach(x =>
                {
                    var mediaLib = mediaLibs.FirstOrDefault(y => x.MediaLibId == y.Id);
                    var mediaLibDirectory = new DirectoryInfo(mediaLib.Path);
                    if (mediaLibDirectory.GetDirectories(x.Id.ToString(), SearchOption.AllDirectories).Length > 0) return;
                    var files = mediaLibDirectory.GetFiles(x.Id.ToString(), SearchOption.AllDirectories);
                    if (mediaLib is null || !mediaLibDirectory.Exists || files.Length <= 0)
                    {
                        dbContext.EncryptedFile.Remove(x);
                    }
                    else
                    {
                        dbContext.CryptoTask.AddRange(files.Select(y => new CryptoTaskEntity { MediaLibId = x.MediaLibId, FullName = y.FullName, Type = Models.CryptoTaskType.Decrypt, IsFolder = false }));
                    }
                });
                await cryptoTaskService.ScanToProcessCryptoTask();
            }

            dbContext.SaveChanges();
        });
    }
}