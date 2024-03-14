using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using PrivateCloud.Server.Services;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Encryption;
using SharpDevLib.Extensions.Jwt;
using SharpDevLib.Extensions.Model;
using System.Text;

namespace PrivateCloud.Server.Controllers;

public class MediaLibController(
    IServiceProvider serviceProvider,
    IRepository<EncryptedFileEntity> encryptedFileRepository,
    IRepository<CryptoTaskEntity> taskRepository,
    IRepository<ThumbEntity> thumbRepository,
    CleanTempService cleanTempService,
    CryptoTaskService cryptoTaskService
    ) : BaseController(serviceProvider)
{
    [HttpGet]
    [Authorize(Roles = StaticNames.AdminName)]
    public PageResult<MediaLibReply> Get([FromQuery] MediaLibQueryRequest request)
    {
        var query = _mediaLibRepository.GetAll();
        if (request.Name.NotEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));

        var all = query.ToList().Where(x => x.AllowedRoles.IsEmpty() || x.AllowedRoles.StringArrayMatch(CurrentUser?.Roles).Count != 0).ToList();
        var count = all.Count;
        var data = all.OrderByDescending(x => x.CreateTime).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        var list = _mapper.Map<List<MediaLibReply>>(data);
        SetMediaLibTaskInfo(list);
        return Result.SucceedPage(list, count, request.PageIndex, request.PageSize);
    }

    [HttpGet]
    [Route("authed")]
    public Result<List<MediaLibReply>> GetAuthedMediaLibList()
    {
        var data = _mediaLibRepository
            .GetAll()
            .OrderBy(x => x.CreateTime)
            .ToList()
            .Where(x => x.AllowedRoles.IsEmpty() || x.AllowedRoles.StringArrayMatch(CurrentUser?.Roles).Count != 0)
            .ToList();
        var list = _mapper.Map<List<MediaLibReply>>(data);
        SetMediaLibTaskInfo(list);
        return Result.Succeed(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = StaticNames.AdminName)]
    public Result<MediaLibReply> Get(Guid id)
    {
        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        var list = new List<MediaLibReply> { _mapper.Map<MediaLibReply>(entity) };
        SetMediaLibTaskInfo(list);
        return Result.Succeed(list.First());
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPost]
    public Result Post([FromBody] MediaLibAddRequest request)
    {
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Path.IsEmpty()) throw new ParameterRequiredException(nameof(request.Path));
        if (!Directory.Exists(request.Path)) throw new PathNotFoundException();
        if (_mediaLibRepository.Any(x => x.Name.ToLower() == request.Name.ToLower())) throw new NameExisteException();
        var path = request.Path.UrlDecode().FormatPath();
        if (_mediaLibRepository.Any(x => x.Path.ToLower() == path.ToLower())) throw new PathExisteException();

        var entity = new MediaLibEntity(request.Name, path, request.AllowedRoles, request.Thumb);
        _mediaLibRepository.Add(entity);
        return Result.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}")]
    public Result Put(Guid id, [FromBody] MediaLibModifyRequest request)
    {
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));
        if (_mediaLibRepository.Any(x => x.Name == request.Name && x.Id != id)) throw new NameExisteException();

        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        var oldEntity = entity.DeepClone();
        entity.Name = request.Name;
        entity.AllowedRoles = request.AllowedRoles;
        entity.Thumb = request.Thumb;
        _mediaLibRepository.Update(entity);
        return Result.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/encrypt")]
    public Result Encrypt(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsEmpty()) throw new PasswordErrorException();
        var password = Encoding.UTF8.GetString(_aes.Decrypt(request.Password, new AesDecryptOption(CurrentUser.CryptoId, CryptoExtension.ZeroAesIVBtyes)));

        if (!entity.IsEncrypt)
        {
            entity.IsEncrypt = true;
            entity.EncryptedKey = entity.Id.PasswordHash(password);
            _mediaLibRepository.Update(entity);
        }
        else
        {
            if (entity.Id.PasswordHash(password) != entity.EncryptedKey) throw new PasswordErrorException();
        }

        cryptoTaskService.ScanToAddCryptoTask(entity, CryptoTaskType.Encrypt);
        return Result.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/decrypt")]
    public Result Decrypt(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsEmpty()) throw new PasswordErrorException();

        var password = Encoding.UTF8.GetString(_aes.Decrypt(request.Password, new AesDecryptOption(CurrentUser.CryptoId.ToString(), CryptoExtension.ZeroAesIVBtyes)));
        if (entity.IsEncrypt)
        {
            if (entity.Id.PasswordHash(password) != entity.EncryptedKey) throw new PasswordErrorException();
            entity.IsEncrypt = false;
            entity.EncryptedKey = null;
            _mediaLibRepository.Update(entity);
        }

        cryptoTaskService.ScanToAddCryptoTask(entity, CryptoTaskType.Decrypt);
        return Result.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/modifyPassword")]
    public Result ModifyPassword(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsEmpty()) throw new PasswordErrorException();
        if (request.NewPassword.IsEmpty()) throw new PasswordErrorException();

        var oldPassword = Encoding.UTF8.GetString(_aes.Decrypt(request.Password, new AesDecryptOption(CurrentUser.CryptoId.ToString(), CryptoExtension.ZeroAesIVBtyes)));
        if (entity.Id.PasswordHash(oldPassword) != entity.EncryptedKey) throw new PasswordErrorException();
        var newPassword = Encoding.UTF8.GetString(_aes.Decrypt(request.NewPassword, new AesDecryptOption(CurrentUser.CryptoId.ToString(), CryptoExtension.ZeroAesIVBtyes)));

        entity.EncryptedKey = entity.Id.PasswordHash(newPassword);
        _mediaLibRepository.Update(entity);
        return Result.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpDelete("{id}")]
    public Result Delete(Guid id)
    {
        var entity = _mediaLibRepository.Get(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        _mediaLibRepository.Remove(entity);

        var encryptedFiles = encryptedFileRepository.GetMany(x => x.MediaLibId == entity.Id).ToList();
        encryptedFileRepository.RemoveRange(encryptedFiles);

        var thumbs = thumbRepository.GetMany(x => x.MediaLibId == entity.Id).ToList();
        thumbRepository.RemoveRange(thumbs);

        cleanTempService.CleanTemp();
        return Result.Succeed();
    }

    [HttpPost]
    [Authorize(Roles = StaticNames.AdminName)]
    [Route("clean")]
    public Result Clean()
    {
        cleanTempService.CleanTemp();
        return Result.Succeed();
    }

    [HttpPost]
    [Authorize(Roles = StaticNames.AdminName)]
    [Route("token")]
    public Result<string> GetToken([FromBody] MediaLibTokenRequest request)
    {
        if (request.Id.IsEmpty()) throw new ParameterRequiredException(nameof(request.Id));
        if (request.Token.IsEmpty()) throw new ParameterRequiredException(nameof(request.Token));

        var password = Encoding.UTF8.GetString(_aes.Decrypt(request.Token, new AesDecryptOption(CurrentUser.CryptoId, CryptoExtension.ZeroAesIVBtyes)));
        var mediaLib = _mediaLibRepository.Get(x => x.Id == request.Id) ?? throw new DataNotFoundException();
        if (request.Id.PasswordHash(password) != mediaLib.EncryptedKey) throw new PasswordErrorException();

        var jwtKey = _configuration.GetValue<string>(StaticNames.JwtKeyName);
        var payload = new MediaLibPayload { MediaLibId = mediaLib.Id, Key = Convert.ToBase64String(_aes.Encrypt(mediaLib.EncryptedKey, new AesEncryptOption(jwtKey, CryptoExtension.ZeroAesIVBtyes))) };
        var jwtToken = _jwtService.Create(new JwtCreateOption(JwtAlgorithm.HS256, jwtKey, payload));
        return Result.Succeed<string>(jwtToken);
    }

    #region Private
    void SetMediaLibTaskInfo(List<MediaLibReply> source)
    {
        if (source.IsEmpty()) return;
        var ids = source.Select(x => x.Id).ToList();
        var tasks = taskRepository.GetMany(x => ids.Contains(x.MediaLibId)).ToList();
        tasks.GroupBy(x => x.MediaLibId).ToList().ForEach(task =>
        {
            var item = source.First(x => x.Id == task.Key);
            item.RunningTaskCount = task.Count(x => x.HandleCount <= Statics.TaskMaxHandleCount);
            item.FailedTaskCount = task.Count(x => x.HandleCount > Statics.TaskMaxHandleCount);
        });
    }
    #endregion
}
