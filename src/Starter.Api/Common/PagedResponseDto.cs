namespace Starter.Api.Common;

public sealed record PagedResponseDto<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount);
