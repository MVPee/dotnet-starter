namespace Starter.IntegrationTests.Contracts;

internal sealed record PagedResponse<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount);
