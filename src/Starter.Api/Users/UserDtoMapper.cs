using Starter.Api.Common;
using Starter.Application.Common.Persistence;
using Starter.Application.Users;
using Starter.Application.Users.CreateUser;
using Starter.Domain.Users;

namespace Starter.Api.Users;

internal static class UserDtoMapper
{
    public static CreateUserCommand ToCommand(this CreateUserDto createUserDto)
        => new(createUserDto.Email, createUserDto.FirstName, createUserDto.LastName);

    public static UserFilter ToFilter(this GetUsersQueryDto getUsersQueryDto)
        => new() { Email = getUsersQueryDto.Email, Page = getUsersQueryDto.Page, PageSize = getUsersQueryDto.PageSize };

    public static UserDto ToDto(this User user)
        => new(user.Id, user.Email, user.FirstName, user.LastName, user.CreatedAt);

    public static PagedResponseDto<UserDto> ToDto(this PagedList<User> pagedUsers)
        => new([.. pagedUsers.Items.Select(user => user.ToDto())], pagedUsers.Page, pagedUsers.PageSize, pagedUsers.TotalCount);
}
