using Starter.Application.Common.Persistence;
using Starter.Domain.Users;

namespace Starter.Application.Users;

public sealed record UserFilter : PagedFilter
{
    // Normalized here so every caller matches the stored (normalized) email; blank means "no filter".
    public string? Email { get; init => field = string.IsNullOrWhiteSpace(value) ? null : User.NormalizeEmail(value); }
}
