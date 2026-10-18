using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Starter.Domain.Common;
using Starter.Domain.Users;
using Starter.Infrastructure.Persistence.Users;

namespace Starter.Infrastructure.Persistence;

// Translates unique-index violations (concurrent duplicates that slipped past use-case checks) into domain errors.
internal static class UniqueConstraintErrors
{
    private static readonly FrozenDictionary<string, Error> ErrorsByConstraintName = new Dictionary<string, Error>
    {
        [UserEntityConfiguration.EmailUniqueIndexName] = UserErrors.EmailAlreadyUsed,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool TryGetError(string? constraintName, [NotNullWhen(true)] out Error? error)
    {
        error = null;
        return constraintName is not null && ErrorsByConstraintName.TryGetValue(constraintName, out error);
    }
}
