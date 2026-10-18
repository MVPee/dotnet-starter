using Starter.Application.Common.Persistence;
using Starter.Domain.Users;

namespace Starter.Application.Users;

public sealed record UserFilter : PagedFilter
{
    public string? Email { get; init => field = string.IsNullOrWhiteSpace(value) ? null : EmailAddressRule.Normalize(value); }
}
