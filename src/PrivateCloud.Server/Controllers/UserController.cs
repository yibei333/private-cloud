using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using PrivateCloud.Server.Exceptions;
using PrivateCloud.Server.Models.Pages;
using SharpDevLib;

namespace PrivateCloud.Server.Controllers;

[Authorize(Roles = StaticNames.AdminName)]
public class UserController(IServiceProvider serviceProvider) : BaseController(serviceProvider)
{
    [HttpGet]
    public PageReply<UserDto> Get([FromQuery] UserQueryRequest request)
    {
        var query = _dbContext.User.AsQueryable();
        if (request.Name.NotNullOrEmpty()) query = query.Where(x => x.Name.ToLower().Contains(request.Name.ToLower()));
        var count = query.Count();
        var data = query.OrderByDescending(x => x.CreateTime).Skip(request.Index * request.Size).Take(request.Size).ToList();
        var result = _mapper.Map<List<UserDto>>(data);
        return PageReply<UserDto>.Succeed(result, count, request);
    }

    [HttpGet("{id}")]
    public DataReply<UserDto> Get(Guid id)
    {
        var entity = _dbContext.User.FirstOrDefault(x => x.Id == id) ?? throw new UserNotFoundException();
        var result = _mapper.Map<UserDto>(entity);
        return DataReply<UserDto>.Succeed(result);
    }

    [HttpPost]
    public EmptyReply Post([FromBody] UserAddRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        if (request.Password.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Password));
        if (_dbContext.User.Any(x => x.Name == request.Name)) throw new NameExisteException();
        var salt = Guid.NewGuid().ToString();
        var password = salt.PasswordHash(request.Password);
        var entity = new UserEntity { Name = request.Name, Password = password, Salt = salt, Roles = request.Roles };
        _dbContext.User.Add(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [HttpPut("{id}")]
    public EmptyReply Put(Guid id, [FromBody] UserModifyRequest request)
    {
        if (request.Name.IsNullOrWhiteSpace()) throw new ParameterRequiredException(nameof(request.Name));
        if (_dbContext.User.Any(x => x.Name == request.Name && x.Id != id)) throw new NameExisteException();
        var entity = _dbContext.User.FirstOrDefault(x => x.Id == id) ?? throw new UserNotFoundException();
        if (request.Password.NotNullOrEmpty())
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
        _dbContext.User.Update(entity);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }

    [HttpDelete("{id}")]
    public EmptyReply Delete(Guid id)
    {
        var entity = _dbContext.User.FirstOrDefault(x => x.Id == id) ?? throw new UserNotFoundException();
        _dbContext.User.Remove(entity);

        var favorites = _dbContext.Favorite.Where(x => x.UserId == id).ToList();
        if (favorites.NotNullOrEmpty()) _dbContext.Favorite.RemoveRange(favorites);

        var histories = _dbContext.Favorite.Where(x => x.UserId == id).ToList();
        if (histories.NotNullOrEmpty()) _dbContext.Favorite.RemoveRange(histories);
        _dbContext.SaveChanges();
        return EmptyReply.Succeed();
    }
}
