using Starter.Application.Common.Persistence;
using Starter.Application.Users;

namespace Starter.UnitTests.Application.Common.Persistence;

public sealed class PagedFilterTests
{
    private const int NegativeValue = -5;
    private const int SecondPage = 2;
    private const int PageSize = 10;
    private const int ExpectedSecondPageSkip = 10;

    [Fact]
    public void Defaults_WithoutValues_UseFirstPageAndDefaultPageSize()
    {
        var filter = new UserFilter();

        Assert.Equal(PagedFilter.FirstPage, filter.Page);
        Assert.Equal(PagedFilter.DefaultPageSize, filter.PageSize);
        Assert.Equal(0, filter.Skip);
    }

    [Fact]
    public void Init_WithOutOfRangeValues_ClampsToBounds()
    {
        var filter = new UserFilter { Page = NegativeValue, PageSize = int.MaxValue };

        Assert.Equal(PagedFilter.FirstPage, filter.Page);
        Assert.Equal(PagedFilter.MaxPageSize, filter.PageSize);
    }

    [Fact]
    public void Init_WithHugePage_KeepsSkipWithinIntRange()
    {
        var filter = new UserFilter { Page = int.MaxValue, PageSize = PagedFilter.MaxPageSize };

        Assert.Equal(PagedFilter.MaxPage, filter.Page);
        Assert.True(filter.Skip >= 0);
    }

    [Fact]
    public void Skip_OnSecondPage_SkipsOnePageOfItems()
    {
        var filter = new UserFilter { Page = SecondPage, PageSize = PageSize };

        Assert.Equal(ExpectedSecondPageSkip, filter.Skip);
    }
}
