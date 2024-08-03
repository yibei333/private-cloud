using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Filters;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using PrivateCloud.Server.Services;
using SharpDevLib;
using SharpDevLib.Cryptography;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace PrivateCloud.Server.Controllers;

public class FileController(
    IServiceProvider serviceProvider,
    CleanTempService cleanTempService,
    ThumbTaskService thumbTaskService,
    CryptoTaskService cryptoTaskService
    ) : BaseController(serviceProvider)
{

    private static readonly Dictionary<string, IdNameDataDto<Guid, byte[]>> _staticCache = [];

    #region FileResult
    [HttpGet("statics/{path}")]
    [AllowAnonymous]
    public FileResult GetStatics(string path)
    {
        if (!_staticCache.TryGetValue(path, out var result))
        {
            var relativePath = Encoding.UTF8.GetString(path.HexStringDecode());
            var absolutPath = AppDomain.CurrentDomain.BaseDirectory.CombinePath($"wwwroot/{relativePath}");
            var fileInfo = new FileInfo(absolutPath);
            if (!fileInfo.Exists) throw new FileNotFoundException();
            result = new IdNameDataDto<Guid, byte[]> { Name = fileInfo.Name, Data = System.IO.File.ReadAllBytes(fileInfo.FullName) };
            _staticCache[path] = result;
        }
        return BuildFileResult(result.Name, result.Data);
    }

    [HttpGet("{idPath}")]
    public FileResult Get(string idPath, [FromQuery] bool tryOpen = false)
    {
        var idPathModel = BuildIdPathModel(idPath, out _);
        var fileInfo = new FileInfo(idPathModel.AbsolutePath);
        if (idPathModel.IsEncrypt && fileInfo.Length >= Statics.BigFileSize) throw new BigEncryptFileNotSupportException();
        return BuildFileStreamResult(idPathModel.Name, System.IO.File.OpenRead(idPathModel.AbsolutePath), tryOpen);
    }

    [HttpGet("m3u8/{id}")]
    public FileResult GetM3U8(Guid id)
    {
        var name = $"{id}.m3u8";
        var path = $"{Statics.TempPath}/{id}/{name}";
        return GetM3U8Info(id, path, name);
    }

    [HttpGet("m3u8/ts/{id}/{number}.ts")]
    public FileResult GetTS(Guid id, int number)
    {
        var name = $"{number}.ts";
        var path = $"{Statics.TempPath}/{id}/ts/{name}";
        return GetM3U8Info(id, path, name);
    }

    [HttpGet("m3u8/key/{id}.key")]
    public FileResult GetM3U8Key(Guid id)
    {
        var name = $"{id}.key";
        var path = $"{Statics.TempPath}/{id}/{name}";
        return GetM3U8Info(id, path, name);
    }

    [HttpGet]
    [Route("thumb/{type}/{idPath}")]
    public FileResult GetThumb(int type, string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out _);
        var thumbPath = idPathModel.GetThumbPath(type, VideoThumbIsGridImage, _dbContext);
        if (thumbPath.IsNullOrWhiteSpace() || !System.IO.File.Exists(thumbPath)) throw new DataNotFoundException();
        var fileName = (idPathModel.IsEncrypt ? thumbPath.Replace(".encrypted", "") : thumbPath).GetFileName();
        return BuildFileStreamResult(fileName, System.IO.File.OpenRead(thumbPath));
    }
    #endregion

    [HttpGet]
    [Route("key/{idPath}")]
    public DataReply<EncryptKeyModel> GetKey(string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out _);
        var id = idPathModel.Name.ToGuid();
        var encryptedFile = _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id) ?? throw new DataNotFoundOrHandlingException();
        using var aes = Aes.Create();
        aes.SetKey(CurrentUser.CryptoId.Utf8Decode());
        aes.SetIV(CryptoExtension.ZeroAesIVBtyes);
        var key = aes.Encrypt(encryptedFile.Key.Utf8Decode()).Base64Encode();
        return DataReply<EncryptKeyModel>.Succeed(new EncryptKeyModel(key, encryptedFile.IV));
    }

    [HttpGet]
    [Route("entries")]
    public PageReply<EntryDto> GetEntries([FromQuery] GetEntriesRequest request)
    {
        var idPathModel = BuildIdPathModel(request.IdPath, out var mediaLib);
        var list = GetEntries(mediaLib, idPathModel, request.Name);
        var encryptedFileIds = list.Select(x => x.Id).Distinct().ToList();
        var encryptedFileNames = _dbContext.EncryptedFile.Where(x => encryptedFileIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToList();
        var idPaths = list.Select(x => x.IdPath).ToList();
        var favorites = _dbContext.Favorite.Where(x => idPaths.Contains(x.IdPath)).Select(x => new { x.Id, x.IdPath }).ToList();
        list.ForEach(x =>
        {
            var encryptedFile = encryptedFileNames.FirstOrDefault(y => x.Id == y.Id);
            var favorite = favorites.FirstOrDefault(y => x.IdPath == y.IdPath);
            if (encryptedFile is not null) x.Name = encryptedFile.Name;
            if (favorite is not null) x.FavoritedId = favorite.Id;
            if (!x.IsFolder && x.IsEncrypt && encryptedFile is null) x.Encrypting = true;
        });

        if (request.SortField == "名称")
        {
            if (request.SortDescending == "是") list = [.. list.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Name)];
            else list = [.. list.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name)];
        }
        else if (request.SortField == "时间")
        {
            if (request.SortDescending == "是") list = [.. list.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Time)];
            else list = [.. list.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Time)];
        }

        var count = list.Count;
        var data = list.Skip(request.Index * request.Size).Take(request.Size).ToList();
        SetEntryThumbInfo(data);
        if (!mediaLib.IsEncrypt) thumbTaskService.ScanTaskToWriteAsync(request.IdPath);
        return PageReply<EntryDto>.Succeed(data, count, request);
    }

    [HttpGet]
    [Route("folder/{idPath}")]
    public DataReply<FolderDto> GetFolder(string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        var reply = new FolderDto(mediaLib, idPathModel);

        //set parents
        if (!reply.IsRoot)
        {
            var parent = new DirectoryInfo(idPathModel.AbsolutePath).Parent;
            var parentIdPath = new IdPath(mediaLib, parent.FullName, true);

            while (parentIdPath.RelativePath.NotNullOrEmpty())
            {
                reply.Parents.Add(new FileParentDto(parentIdPath));
                parent = parent.Parent;
                parentIdPath = new IdPath(mediaLib, parent.FullName, true);
            }
            reply.Parents.Reverse();
        }

        //set encrypted file name
        var ids = reply.Parents.Select(x => x.Id).ToList();
        ids.Add(reply.Id);
        ids = ids.Distinct().ToList();
        if (ids.NotNullOrEmpty() && ids.Any(x => x.NotEmpty()))
        {
            var encryptedFileNames = _dbContext.EncryptedFile.Where(x => ids.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToList();
            reply.Name = encryptedFileNames.FirstOrDefault(x => x.Id == reply.Id)?.Name ?? reply.Name;
            reply.Parents.ForEach(x => x.Name = encryptedFileNames.FirstOrDefault(y => y.Id == x.Id)?.Name ?? x.Name);
        }
        return DataReply<FolderDto>.Succeed(reply);
    }

    [HttpGet]
    [Route("file")]
    public DataReply<FileDto> GetFile([FromQuery] GetFileRequest request)
    {
        var idPathModel = BuildIdPathModel(request.IdPath, out var mediaLib);
        var result = new FileDto { Current = new EntryDto(idPathModel) };
        result.Current.ParentIdPath = new IdPath(mediaLib, new FileInfo(idPathModel.AbsolutePath).Directory.FullName, true).Value;
        SetEntryThumbInfo([result.Current]);
        result.Current.Name = _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == result.Current.Id)?.Name ?? result.Current.Name;
        var favorite = _dbContext.Favorite.FirstOrDefault(x => x.IdPath == request.IdPath && x.UserId == CurrentUser.Id);
        result.Current.FavoritedId = favorite?.Id ?? Guid.Empty;
        var history = _dbContext.History.FirstOrDefault(x => x.IdPath == request.IdPath && x.UserId == CurrentUser.Id);
        result.Current.Position = history?.Position;

        var parentIdPath = new IdPath(mediaLib, new FileInfo(idPathModel.AbsolutePath).Directory.FullName, true);
        var entries = GetEntries(mediaLib, parentIdPath, string.Empty);
        if (request.SortField == "名称")
        {
            if (request.SortDescending == "是") entries = [.. entries.OrderByDescending(x => x.Name)];
            else entries = [.. entries.OrderBy(x => x.Name)];
        }
        else if (request.SortField == "时间")
        {
            if (request.SortDescending == "是") entries = [.. entries.OrderByDescending(x => x.Time)];
            else entries = [.. entries.OrderBy(x => x.Time)];
        }

        result.Total = entries.Count;
        result.Index = 1;
        if (entries.Count > 1)
        {
            var currentIndex = entries.IndexOf(entries.First(x => x.Name == idPathModel.Name));
            if (currentIndex > 0) result.Pre = new EntryDto(new IdPath(mediaLib, entries[currentIndex - 1].AbsolutePath, false));
            if (currentIndex < entries.Count - 1) result.Next = new EntryDto(new IdPath(mediaLib, entries[currentIndex + 1].AbsolutePath, false));
            result.Index = currentIndex + 1;
        }
        return DataReply<FileDto>.Succeed(result);
    }

    [HttpPost]
    [Route("folder")]
    public EmptyReply CreateFolder([FromBody] CreateFolderRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        var idPathModel = BuildIdPathModel(request.IdPath, out var mediaLib);
        var directoryInfo = new DirectoryInfo(idPathModel.AbsolutePath.CombinePath(request.Name));
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            if (idPathModel.IsEncrypt) cryptoTaskService.ScanToAddCryptoTask(mediaLib, CryptoTaskType.Encrypt);
        }
        return EmptyReply.Succeed();
    }

    [HttpPut]
    [Route("rename/{idPath}")]
    public DataReply<string> Rename(string idPath, [FromBody] RenameRequest request)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        var id = idPathModel.Name.ToGuid();
        if (request.IsFolder)
        {
            var directoryInfo = new DirectoryInfo(idPathModel.AbsolutePath);
            var encryptedFile = _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id);
            if (encryptedFile is null)
            {
                if (request.Name == directoryInfo.Name) return DataReply<string>.Succeed(idPath);
                directoryInfo.MoveTo(directoryInfo.Parent.FullName.CombinePath(request.Name));
                return DataReply<string>.Succeed(new IdPath(mediaLib, directoryInfo.FullName, true).Value);
            }
            else
            {
                encryptedFile.Name = request.Name;
                _dbContext.EncryptedFile.Update(encryptedFile);
                _dbContext.SaveChanges();
                return DataReply<string>.Succeed(idPath);
            }
        }
        else
        {
            var fileInfo = new FileInfo(idPathModel.AbsolutePath);
            var encryptedFile = _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id);
            if (encryptedFile is null)
            {
                if (request.Name == fileInfo.Name) return DataReply<string>.Succeed(idPath);
                fileInfo.MoveTo(fileInfo.Directory.FullName.CombinePath(request.Name));
                return DataReply<string>.Succeed(new IdPath(mediaLib, fileInfo.FullName, false).Value);
            }
            else
            {
                encryptedFile.Name = request.Name;
                _dbContext.EncryptedFile.Update(encryptedFile);
                _dbContext.SaveChanges();
                return DataReply<string>.Succeed(idPath);
            }
        }
    }

    [HttpPost]
    [Route("upload/{idPath}")]
    [DisableRequestSizeLimit]
    [MultipartFormData]
    [DisableFormValueModelBinding]
    public async Task<DataReply<List<NameIdPathDto>>> UploadFile(string idPath)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);
        var contentTypeHeader = MediaTypeHeaderValue.Parse(HttpContext.Request.ContentType).ToString();
        var boundary = contentTypeHeader[(contentTypeHeader.IndexOf(StaticNames.BoundarySymbol) + StaticNames.BoundarySymbol.Length)..];
        var multipartReader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await multipartReader.ReadNextSectionAsync();
        var reply = new List<NameIdPathDto>();

        while (section != null)
        {
            var fileSection = section.AsFileSection();
            if (fileSection != null)
            {
                var fullPath = idPathModel.AbsolutePath.CombinePath(fileSection.FileName.UrlDecode().Utf8Encode());
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Exists) fileInfo.Delete();

                var directory = fileInfo.Directory;
                if (!directory.Exists) directory.Create();

                using var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fileSection.FileStream.Seek(0, SeekOrigin.Begin);
                await fileSection.FileStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close();
                reply.Add(new NameIdPathDto { Name = fileSection.FileName.UrlDecode().Utf8Encode(), IdPath = new IdPath(mediaLib, fullPath, false).Value });
            }
            section = await multipartReader.ReadNextSectionAsync();
        }
        if (!mediaLib.IsEncrypt) thumbTaskService.ScanTaskToWriteAsync(idPath);
        else cryptoTaskService.ScanToAddCryptoTask(mediaLib, CryptoTaskType.Encrypt);
        return DataReply<List<NameIdPathDto>>.Succeed(reply);
    }

    [HttpDelete("{idPath}/{isFolder}")]
    public EmptyReply Delete(string idPath, bool isFolder)
    {
        var idPathModel = BuildIdPathModel(idPath, out var mediaLib);

        //遗留问题:sqlite本身是否支持事务?如何保证数据库操作和文件操作在一个事务中【https://github.com/chinhdo/txFileManager】
        if (isFolder)
        {
            DeleteFolder(idPathModel, mediaLib);
            new DirectoryInfo(idPathModel.AbsolutePath).Delete(true);
        }
        else
        {
            DeleteFile(idPathModel);
            new FileInfo(idPathModel.AbsolutePath).Delete();
        }

        cleanTempService.CleanTemp();
        return EmptyReply.Succeed();
    }

    #region Private
    FileResult GetM3U8Info(Guid id, string path, string name)
    {
        if (id.IsEmpty()) throw new NotSupportedException();
        var mediaLibId = _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id)?.MediaLibId ?? throw new NotSupportedException();
        var mediaLib = _dbContext.MediaLib.FirstOrDefault(x => x.Id == mediaLibId) ?? throw new NotSupportedException();
        EnsureMediaLibAuth(mediaLib);
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists) throw new NotSupportedException();
        return BuildFileStreamResult(name, fileInfo.OpenRead());
    }

    List<EntryDto> GetEntries(MediaLibEntity mediaLib, IdPath idPathModel, string name)
    {
        var directory = new DirectoryInfo(idPathModel.AbsolutePath);
        if (name.IsNullOrWhiteSpace()) return (from a in directory.GetDirectories() select new EntryDto(new IdPath(mediaLib, a.FullName, true))).Union(from a in directory.GetFiles() select new EntryDto(new IdPath(mediaLib, a.FullName, false))).ToList();

        if (idPathModel.IsEncrypt)
        {
            var result = new List<EntryDto>();
            var names = _dbContext.EncryptedFile.Where(x => x.MediaLibId == idPathModel.MediaLibId && x.Name.ToLower().Contains(name.ToLower())).Select(x => $"*{x.Id}*").ToList();
            names.ForEach(name =>
            {
                var entries = (from a in directory.GetDirectories(name, SearchOption.AllDirectories) select new EntryDto(new IdPath(mediaLib, a.FullName, true))).Union(from a in directory.GetFiles(name, SearchOption.AllDirectories) select new EntryDto(new IdPath(mediaLib, a.FullName, false))).ToList();
                result.AddRange(entries);
            });
            return result;
        }
        else
        {
            var pattern = $"*{name}*";
            return (from a in directory.GetDirectories(pattern, SearchOption.AllDirectories) select new EntryDto(new IdPath(mediaLib, a.FullName, true))).Union(from a in directory.GetFiles(pattern, SearchOption.AllDirectories) select new EntryDto(new IdPath(mediaLib, a.FullName, false))).ToList();
        }
    }

    void SetEntryThumbInfo(List<EntryDto> entries)
    {
        if (entries.IsNullOrEmpty()) return;
        var idPathList = entries.Where(x => !x.IsEncrypt).Select(x => x.IdPath).ToList();
        var thumbs = _dbContext.Thumb.Where(x => idPathList.Contains(x.IdPath)).ToList();
        entries.ForEach(entry =>
        {
            if (entry.Name.IsImage() || entry.Name.IsVideo() || entry.Name.IsGif())
            {
                var extension = entry.IsEncrypt ? ".encrypted" : "";
                var id = entry.Id;
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

    void DeleteFile(IdPath idPathModel)
    {
        var thumb = _dbContext.Thumb.FirstOrDefault(x => x.IdPath == idPathModel.Value);
        if (thumb is not null)
        {
            _dbContext.Thumb.Remove(thumb);
        }

        var id = idPathModel.Name.ToGuid();
        var encryptedFile = id.IsEmpty() ? null : _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id);
        if (encryptedFile is not null)
        {
            _dbContext.EncryptedFile.Remove(encryptedFile);
        }
        _dbContext.SaveChanges();
    }

    void DeleteFolder(IdPath idPathModel, MediaLibEntity mediaLib)
    {
        var id = idPathModel.Name.ToGuid();
        var encryptedFile = id.IsEmpty() ? null : _dbContext.EncryptedFile.FirstOrDefault(x => x.Id == id);
        if (encryptedFile is not null)
        {
            _dbContext.EncryptedFile.Remove(encryptedFile);
            _dbContext.SaveChanges();
        }

        var directoryInfo = new DirectoryInfo(idPathModel.AbsolutePath);
        directoryInfo.GetDirectories().ToList().ForEach(x => DeleteFolder(new IdPath(mediaLib, x.FullName, true), mediaLib));
        directoryInfo.GetFiles().ToList().ForEach(x => DeleteFile(new IdPath(mediaLib, x.FullName, false)));
    }
    #endregion
}
