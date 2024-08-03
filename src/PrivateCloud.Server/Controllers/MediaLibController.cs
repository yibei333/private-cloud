using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models;
using PrivateCloud.Server.Models.Pages;
using PrivateCloud.Server.Services;
using SharpDevLib;
using SharpDevLib.Cryptography;
using System.Security.Cryptography;

namespace PrivateCloud.Server.Controllers;

public class MediaLibController(
    IServiceProvider serviceProvider,
    CleanTempService cleanTempService,
    CryptoTaskService cryptoTaskService
    ) : BaseController(serviceProvider)
{
    [HttpGet]
    [Authorize(Roles = StaticNames.AdminName)]
    public PageReply<MediaLibDto> Get([FromQuery] MediaLibQueryRequest request)
    {
        var query = _dbContext.MediaLib.AsQueryable();
        if (request.Name.NotNullOrWhiteSpace()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));

        var all = query.ToList().Where(x => x.AllowedRoles.IsNullOrWhiteSpace() || x.AllowedRoles.StringArrayMatch(CurrentUser?.Roles).Count != 0).ToList();
        var count = all.Count;
        var data = all.OrderByDescending(x => x.CreateTime).Skip(request.Index * request.Size).Take(request.Size).ToList();

        var list = _mapper.Map<List<MediaLibDto>>(data);
        SetMediaLibTaskInfo(list);
        return PageReply<MediaLibDto>.Succeed(list, count, request);
    }

    [HttpGet]
    [Route("authed")]
    public DataReply<List<MediaLibDto>> GetAuthedMediaLibList()
    {
        var data = _dbContext.MediaLib
            .OrderBy(x => x.CreateTime)
            .ToList()
            .Where(x => x.AllowedRoles.IsNullOrWhiteSpace() || x.AllowedRoles.StringArrayMatch(CurrentUser?.Roles).Count != 0)
            .ToList();
        var list = _mapper.Map<List<MediaLibDto>>(data);
        SetMediaLibTaskInfo(list);
        return DataReply<List<MediaLibDto>>.Succeed(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = StaticNames.AdminName)]
    public DataReply<MediaLibDto> Get(Guid id)
    {
        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        var list = new List<MediaLibDto> { _mapper.Map<MediaLibDto>(entity) };
        SetMediaLibTaskInfo(list);
        return DataReply<MediaLibDto>.Succeed(list.First());
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPost]
    public EmptyReply Post([FromBody] MediaLibAddRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Path.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Path));
        if (!Directory.Exists(request.Path)) throw new PathNotFoundException();
        if (_dbContext.MediaLib.Any(x => x.Name.ToLower() == request.Name.ToLower())) throw new NameExisteException();
        var path = request.Path.UrlDecode().Utf8Encode().FormatPath();
        if (_dbContext.MediaLib.Any(x => x.Path.ToLower() == path.ToLower())) throw new PathExisteException();

        var entity = new MediaLibEntity(request.Name, path, request.AllowedRoles, request.Thumb);
        _dbContext.MediaLib.Add(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}")]
    public EmptyReply Put(Guid id, [FromBody] MediaLibModifyRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        if (_dbContext.MediaLib.Any(x => x.Name == request.Name && x.Id != id)) throw new NameExisteException();

        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        var oldEntity = entity.DeepClone();
        entity.Name = request.Name;
        entity.AllowedRoles = request.AllowedRoles;
        entity.Thumb = request.Thumb;
        _dbContext.MediaLib.Update(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/encrypt")]
    public EmptyReply Encrypt(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsNullOrWhiteSpace()) throw new PasswordErrorException();
        using var aes = Aes.Create();
        aes.SetKey(CurrentUser.CryptoId.Utf8Decode());
        aes.SetIV(CryptoExtension.ZeroAesIVBtyes);
        var password = aes.Decrypt(request.Password.Base64Decode()).Utf8Encode();

        if (!entity.IsEncrypt)
        {
            entity.IsEncrypt = true;
            entity.EncryptedKey = entity.Id.PasswordHash(password);
            _dbContext.MediaLib.Update(entity);
            _dbContext.SaveChanges();
        }
        else
        {
            if (entity.Id.PasswordHash(password) != entity.EncryptedKey) throw new PasswordErrorException();
        }

        cryptoTaskService.ScanToAddCryptoTask(entity, CryptoTaskType.Encrypt);
        return EmptyReply.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/decrypt")]
    public EmptyReply Decrypt(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsNullOrWhiteSpace()) throw new PasswordErrorException();

        using var aes = Aes.Create();
        aes.SetKey(CurrentUser.CryptoId.Utf8Decode());
        aes.SetIV(CryptoExtension.ZeroAesIVBtyes);
        var password = aes.Decrypt(request.Password.Base64Decode()).Utf8Encode();

        if (entity.IsEncrypt)
        {
            if (entity.Id.PasswordHash(password) != entity.EncryptedKey) throw new PasswordErrorException();
            entity.IsEncrypt = false;
            entity.EncryptedKey = null;
            _dbContext.MediaLib.Update(entity);
            _dbContext.SaveChanges();
        }

        cryptoTaskService.ScanToAddCryptoTask(entity, CryptoTaskType.Decrypt);
        return EmptyReply.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpPut("{id}/modifyPassword")]
    public EmptyReply ModifyPassword(Guid id, [FromBody] MediaLibCryptoRequest request)
    {
        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        if (request.Password.IsNullOrWhiteSpace()) throw new PasswordErrorException();
        if (request.NewPassword.IsNullOrWhiteSpace()) throw new PasswordErrorException();

        using var aes = Aes.Create();
        aes.SetKey(CurrentUser.CryptoId.ToString().Utf8Decode());
        aes.SetIV(CryptoExtension.ZeroAesIVBtyes);

        var oldPassword = aes.Decrypt(request.Password.Utf8Decode()).Utf8Encode();
        if (entity.Id.PasswordHash(oldPassword) != entity.EncryptedKey) throw new PasswordErrorException();
        var newPassword = aes.Decrypt(request.NewPassword.Utf8Decode()).Utf8Encode();

        entity.EncryptedKey = entity.Id.PasswordHash(newPassword);
        _dbContext.MediaLib.Update(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [Authorize(Roles = StaticNames.AdminName)]
    [HttpDelete("{id}")]
    public EmptyReply Delete(Guid id)
    {
        var entity = _dbContext.MediaLib.FirstOrDefault(x => x.Id == id) ?? throw new MediaLibNotFoundException();
        _dbContext.MediaLib.Remove(entity);

        var encryptedFiles = _dbContext.EncryptedFile.Where(x => x.MediaLibId == entity.Id).ToList();
        _dbContext.EncryptedFile.RemoveRange(encryptedFiles);

        var thumbs = _dbContext.Thumb.Where(x => x.MediaLibId == entity.Id).ToList();
        _dbContext.Thumb.RemoveRange(thumbs);

        _dbContext.SaveChanges();
        cleanTempService.CleanTemp();
        return EmptyReply.Succeed();
    }

    [HttpPost]
    [Authorize(Roles = StaticNames.AdminName)]
    [Route("clean")]
    public EmptyReply Clean()
    {
        cleanTempService.CleanTemp();
        return EmptyReply.Succeed();
    }

    [HttpPost]
    [Route("token")]
    public DataReply<string> GetToken([FromBody] MediaLibTokenRequest request)
    {
        if (request.Id.IsEmpty()) throw new ParameterRequiredException(nameof(request.Id));
        if (request.Token.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Token));

        using var aes = Aes.Create();
        aes.SetKey(CurrentUser.CryptoId.ToString().Utf8Decode());
        aes.SetIV(CryptoExtension.ZeroAesIVBtyes);
        var password = aes.Decrypt(request.Token.Base64Decode()).Utf8Encode();
        var mediaLib = _dbContext.MediaLib.FirstOrDefault(x => x.Id == request.Id) ?? throw new DataNotFoundException();
        if (request.Id.PasswordHash(password) != mediaLib.EncryptedKey) throw new PasswordErrorException();

        var jwtKey = _configuration.GetValue<string>(StaticNames.JwtKeyName);
        using var jwtAes = Aes.Create();
        jwtAes.SetKey(jwtKey.Utf8Decode());
        jwtAes.SetIV(CryptoExtension.ZeroAesIVBtyes);
        var encryptedJwtKey = jwtAes.Encrypt(mediaLib.EncryptedKey.Utf8Decode()).Base64Encode();
        var payload = new MediaLibPayload { MediaLibId = mediaLib.Id, Key = encryptedJwtKey };
        var jwtToken = Jwt.Create(new JwtCreateWithHMACSHA256Request(payload, jwtKey.Utf8Decode()));
        return DataReply<string>.Succeed(jwtToken);
    }

    #region Private
    void SetMediaLibTaskInfo(List<MediaLibDto> source)
    {
        if (source.IsNullOrEmpty()) return;
        var ids = source.Select(x => x.Id).ToList();
        var tasks = _dbContext.CryptoTask.Where(x => ids.Contains(x.MediaLibId)).ToList();
        tasks.GroupBy(x => x.MediaLibId).ToList().ForEach(task =>
        {
            var item = source.First(x => x.Id == task.Key);
            item.RunningTaskCount = task.Count(x => x.HandleCount <= Statics.TaskMaxHandleCount);
            item.FailedTaskCount = task.Count(x => x.HandleCount > Statics.TaskMaxHandleCount);
        });
    }
    #endregion
}
