namespace Starter.Application.Common.Persistence;

public sealed record PagedList<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount);
