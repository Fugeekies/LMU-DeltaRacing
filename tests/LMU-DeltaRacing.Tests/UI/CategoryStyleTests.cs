using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the color class of each category.
/// </summary>
[TestClass]
public sealed class CategoryStyleTests
{
    [TestMethod]
    [DataRow("Hypercar", "category-hypercar")]
    [DataRow("LMP2", "category-lmp2")]
    [DataRow("LMP3", "category-lmp3")]
    [DataRow("LMGTE", "category-lmgte")]
    [DataRow("LMGT3", "category-lmgt3")]
    public void Each_known_category_has_its_class(string category, string expected)
    {
        CategoryStyle.CssClass(category).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("GT4")]
    [DataRow("—")]
    public void Unknown_classes_stay_neutral(string category)
    {
        CategoryStyle.CssClass(category).Should().Be("category-other");
    }
}
