using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the segments of the stacked bars of the home page and their hover popups.
/// </summary>
[TestClass]
public sealed class HomeBarLayoutTests
{
    [TestMethod]
    public void A_segment_shows_its_value_and_its_share_of_the_total()
    {
        var segments = HomeBarLayout.Create([new HomeBarPart("LMGT3", 0.4828, "42")]);

        segments.Should().ContainSingle().Which.Tooltip.Should().Be("LMGT3 : 42 (48.3 %)");
    }

    [TestMethod]
    public void Segments_keep_their_order_and_their_width()
    {
        var segments = HomeBarLayout.Create(
        [
            new HomeBarPart("LMP2", 0.25, "1"),
            new HomeBarPart("LMGT3", 0.75, "3")
        ]);

        segments.Select(segment => segment.Label).Should().Equal("LMP2", "LMGT3");
        segments.Select(segment => segment.Width).Should().Equal("25%", "75%");
    }

    [TestMethod]
    public void The_popup_follows_the_center_of_its_segment()
    {
        var segments = HomeBarLayout.Create(
        [
            new HomeBarPart("LMP2", 0.2, "1"),
            new HomeBarPart("LMP3", 0.5, "2"),
            new HomeBarPart("LMGT3", 0.3, "3")
        ]);

        segments.Select(segment => segment.Center).Should().Equal("10%", "45%", "85%");
    }

    [TestMethod]
    public void Percentages_do_not_depend_on_the_language_of_Windows()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            HomeBarLayout.Create([new HomeBarPart("LMP2", 0.125, "1")])
                .Single().Width.Should().Be("12.5%");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }
}
