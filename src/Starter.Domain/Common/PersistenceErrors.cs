namespace Starter.Domain.Common;

public static class PersistenceErrors
{
    public const string ConcurrencyConflictCode = "persistence.concurrency_conflict";

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        ConcurrencyConflictCode,
        "The data was modified or deleted by another operation. Reload it and try again.");
}
