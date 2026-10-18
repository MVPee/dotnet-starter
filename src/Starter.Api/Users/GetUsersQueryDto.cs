using System.ComponentModel.DataAnnotations;
using Starter.Application.Common.Persistence;
using Starter.Domain.Users;

namespace Starter.Api.Users;

public sealed record GetUsersQueryDto
{
    [MaxLength(UserConstraints.EmailMaxLength)]
    public string? Email { get; init; }

    [Range(PagedFilter.FirstPage, PagedFilter.MaxPage)]
    public int Page { get; init; } = PagedFilter.FirstPage;

    [Range(PagedFilter.MinPageSize, PagedFilter.MaxPageSize)]
    public int PageSize { get; init; } = PagedFilter.DefaultPageSize;
}
