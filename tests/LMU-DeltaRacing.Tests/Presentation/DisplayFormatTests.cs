using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the value formats shared by the dashboard pages and the console views.
/// </summary>
[TestClass]
public sealed class DisplayFormatTests
{
    /// <summary>
    /// Verifies lap, total and delta times, and the dash shown for a missing value.
    /// </summary>
    [TestMethod]
    public void Times_use_invariant_formats_and_a_dash_when_missing()
    {
        DisplayFormat.Lap(TimeSpan.FromSeconds(122.451)).Should().Be("2:02.451");
        DisplayFormat.Lap(null).Should().Be("—");
        DisplayFormat.TotalTime(TimeSpan.FromSeconds(1501.432)).Should().Be("25:01.432");
        DisplayFormat.TotalTime(TimeSpan.FromSeconds(3725.5)).Should().Be("1:02:05.500");
        DisplayFormat.TotalTime(null).Should().Be("—");
        DisplayFormat.Delta(TimeSpan.FromSeconds(-0.245)).Should().Be("-0.245s");
        DisplayFormat.Delta(TimeSpan.FromSeconds(0.102)).Should().Be("+0.102s");
        DisplayFormat.Delta(TimeSpan.Zero).Should().Be("0.000s");
        DisplayFormat.Delta(null).Should().Be("—");
    }

    /// <summary>
    /// Verifies the number formats of the home dashboard: narrow no-break space between thousands,
    /// point decimals.
    /// </summary>
    [TestMethod]
    public void Numbers_use_narrow_space_thousands_and_point_decimals()
    {
        DisplayFormat.Count(1342).Should().Be("1 342");
        DisplayFormat.Count(null).Should().Be("—");
        DisplayFormat.Average(8.44).Should().Be("8.4");
        DisplayFormat.Signed(0.94, "0.0").Should().Be("+0.9");
        DisplayFormat.Signed(-0.4, "0.0").Should().Be("−0.4");
        DisplayFormat.Signed(0, "0").Should().Be("0");
        DisplayFormat.Distance(6120.4).Should().Be("6 120 km");
        DisplayFormat.Duration(41 * 3600 + 37 * 60 + 12).Should().Be("41:37");
        DisplayFormat.Percent(0.0574).Should().Be("5.7 %");
    }

    /// <summary>
    /// Verifies that dates are shown in local time.
    /// </summary>
    [TestMethod]
    public void Dates_are_shown_in_local_time()
    {
        var startedAt = new DateTimeOffset(2026, 10, 16, 19, 0, 0, TimeSpan.Zero);
        var local = startedAt.ToLocalTime();

        DisplayFormat.DateTime(startedAt).Should().Be(local.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture));
        DisplayFormat.Day(startedAt).Should().Be(local.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture));
    }
}
