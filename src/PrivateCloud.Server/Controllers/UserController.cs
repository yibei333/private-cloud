using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Server.Controllers;

[Authorize(Roles = StaticNames.AdminName)]
public class UserController(
    IServiceProvider serviceProvider,
    IRepository<UserEntity> repository,
    IRepository<HistoryEntity> historyRepository,
    IRepository<FavoriteEntity> favoriteRepository
    ) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageResult<UserReply> Get([FromQuery] UserQueryRequest request)
    {
        var query = repository.GetAll();
        if (request.Name.NotEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));
        var count = query.Count();
        var data = query.OrderByDescending(x => x.CreateTime).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();
        var result = _mapper.Map<List<UserReply>>(data);
        return Result.SucceedPage(result, count, request.PageIndex, request.PageSize);
    }

    [HttpGet("{id}")]
    public Result<UserReply> Get(Guid id)
    {
        var entity = repository.Get(x => x.Id == id) ?? throw new UserNotFoundException();
        var result = _mapper.Map<UserReply>(entity);
        return Result.Succeed(result);
    }

    [HttpPost]
    public Result Post([FromBody] UserAddRequest request)
    {
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Password.IsEmpty()) throw new ParameterRequiredException(nameof(request.Password));
        if (repository.Any(x => x.Name == request.Name)) throw new NameExisteException();
        var salt = Guid.NewGuid().ToString();
        var password = salt.PasswordHash(request.Password);
        var entity = new UserEntity { Name = request.Name, Password = password, Salt = salt, Roles = request.Roles };
        repository.Add(entity);
        return Result.Succeed();
    }

    [HttpPut("{id}")]
    public Result Put(Guid id, [FromBody] UserModifyRequest request)
    {
        if (request.Name.IsEmpty()) throw new ParameterRequiredException(nameof(request.Name));
        if (repository.Any(x => x.Name == request.Name && x.Id != id)) throw new NameExisteException();
        var entity = repository.Get(x => x.Id == id) ?? throw new UserNotFoundException();
        if (request.Password.NotEmpty())
        {
            var salt = Guid.NewGuid().ToString();
            var password = salt.PasswordHash(request.Password); ;
            entity.Salt = salt;
            entity.Password = password;
        }
        if (!request.IsForbidden) entity.LoginFailCount = 0;
        entity.Name = request.Name;
        entity.Roles = request.Roles;
        entity.IsForbidden = request.IsForbidden;
        repository.Update(entity);
        return Result.Succeed();
    }

    [HttpDelete("{id}")]
    public Result Delete(Guid id)
    {
        var entity = repository.Get(x => x.Id == id) ?? throw new UserNotFoundException();
        repository.Remove(entity);

        var favorites = favoriteRepository.GetMany(x => x.UserId == id).ToList();
        if (favorites.NotEmpty()) favoriteRepository.RemoveRange(favorites);

        var histories = historyRepository.GetMany(x => x.UserId == id).ToList();
        if (histories.NotEmpty()) historyRepository.RemoveRange(histories);
        return Result.Succeed();
    }
}
