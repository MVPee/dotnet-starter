namespace Starter.Application.Common.Persistence;

public abstract record PagedFilter
{
    public const int FirstPage = 1;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    // Keeps (Page - 1) * PageSize within int range for Skip().
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public int Page { get; init => field = Math.Clamp(value, FirstPage, MaxPage); } = FirstPage;

    public int PageSize { get; init => field = Math.Clamp(value, MinPageSize, MaxPageSize); } = DefaultPageSize;

    public int Skip => (Page - FirstPage) * PageSize;
}
