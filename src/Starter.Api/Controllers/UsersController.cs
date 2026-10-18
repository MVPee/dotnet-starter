using Microsoft.AspNetCore.Mvc;
using Starter.Api.Common;
using Starter.Api.Errors;
using Starter.Api.Users;
using Starter.Application.Users.CreateUser;
using Starter.Application.Users.GetUserById;
using Starter.Application.Users.GetUsers;

namespace Starter.Api.Controllers;

[ApiController]
[Route("api/users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UsersController(
    CreateUserUseCase createUserUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    GetUsersUseCase getUsersUseCase) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] GetUsersQueryDto getUsersQueryDto, CancellationToken cancellationToken)
    {
        var pagedUsers = await getUsersUseCase.ExecuteAsync(getUsersQueryDto.ToFilter(), cancellationToken);
        return Ok(pagedUsers.ToDto());
    }

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUserDto createUserDto, CancellationToken cancellationToken)
    {
        var result = await createUserUseCase.ExecuteAsync(createUserDto.ToCommand(), cancellationToken);
        return result.Match<IActionResult>(
            user => CreatedAtAction(nameof(GetById), new { id = user.Id }, user.ToDto()),
            this.ToProblem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getUserByIdUseCase.ExecuteAsync(id, cancellationToken);
        return result.Match<IActionResult>(user => Ok(user.ToDto()), this.ToProblem);
    }
}
