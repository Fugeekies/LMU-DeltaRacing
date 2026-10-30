using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the links that open the progression page on a circuit and a category.
/// </summary>
[TestClass]
public sealed class ProgressionLinkTests
{
    [TestMethod]
    public void The_link_selects_the_circuit_and_the_category()
    {
        ProgressionLink.For("Spa", "GP", "LMP2").Should().Be("/progression?venue=Spa&course=GP&category=LMP2");
    }

    [TestMethod]
    public void Names_are_escaped()
    {
        ProgressionLink.For("Circuit de la Sarthe", "24h & GP", "LMGT3")
            .Should().Be("/progression?venue=Circuit%20de%20la%20Sarthe&course=24h%20%26%20GP&category=LMGT3");
    }
}
