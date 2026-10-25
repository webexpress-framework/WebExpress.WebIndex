using WebExpress.WebIndex.WiUI.Explorer;
using Xunit;

namespace WebExpress.WebIndex.WiUI.Test;

/// <summary>Protects paging from repeating full-result sorting and filtering work.</summary>
public class UnitTestExplorerTableProjection
{
    /// <summary>Reuses a projection for page changes and recomputes it when its inputs change.</summary>
    [Fact]
    public void Select_ReusesProjectionUntilInputsChange()
    {
        // arrange
        var rows = new object[] { new ExplorerTestItem { Name = "beta", Rank = 2 }, new ExplorerTestItem { Name = "alpha", Rank = 1 } };
        var projection = new ExplorerTableProjection();
        var first = projection.Select(rows, ["Name", "Rank"], "a", "Rank", false);

        // act
        var repeated = projection.Select(rows, ["Name", "Rank"], "a", "Rank", false);

        // validation
        Assert.Same(first, repeated);
        Assert.Equal(1, ExplorerTable.Value(first[0], "Rank"));
        var reversed = projection.Select(rows, ["Name", "Rank"], "a", "Rank", true);
        Assert.Equal(2, ExplorerTable.Value(reversed[0], "Rank"));
        Assert.Single(projection.Select(rows, ["Name"], "alpha", null, false));
        Assert.Empty(projection.Select(rows, ["Rank"], "alpha", null, false));
        Assert.Single(projection.Select([rows[0]], ["Name"], "", null, false));
        Assert.Same(rows, projection.Select(rows, ["Name"], "", null, false));
    }
}
