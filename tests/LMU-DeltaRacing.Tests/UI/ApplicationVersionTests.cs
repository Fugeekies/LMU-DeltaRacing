using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies how the application version is displayed and compared with the release tags.
/// </summary>
[TestClass]
public sealed class ApplicationVersionTests
{
    [TestMethod]
    public void Display_prefixes_the_version_like_a_release_tag()
    {
        ApplicationVersion.Display("0.1.2").Should().Be("v0.1.2");
    }

    [TestMethod]
    public void Release_versions_and_tags_are_recognised()
    {
        ApplicationVersion.TryParseRelease("0.1.2", out var version).Should().BeTrue();
        version.Should().Be(new Version(0, 1, 2));

        ApplicationVersion.TryParseRelease("v12.0.3", out version).Should().BeTrue();
        version.Should().Be(new Version(12, 0, 3));
    }

    [TestMethod]
    [DataRow("0.0.0-dev")]
    [DataRow("0.1")]
    [DataRow("0.1.2.3")]
    [DataRow("latest")]
    [DataRow("")]
    [DataRow(null)]
    public void Anything_else_is_not_a_release(string? value)
    {
        ApplicationVersion.TryParseRelease(value, out _).Should().BeFalse();
    }

    [TestMethod]
    public void Versions_are_compared_by_number_and_not_as_text()
    {
        ApplicationVersion.TryParseRelease("v0.10.0", out var newer).Should().BeTrue();
        ApplicationVersion.TryParseRelease("0.9.0", out var older).Should().BeTrue();

        newer.Should().BeGreaterThan(older!);
    }

    [TestMethod]
    public void A_local_build_is_a_development_version()
    {
        ApplicationVersion.Current.Should().Be("0.0.0-dev");
    }
}
