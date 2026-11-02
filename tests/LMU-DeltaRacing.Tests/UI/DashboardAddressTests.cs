using FluentAssertions;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the address shown to the user in the browser.
/// </summary>
[TestClass]
public sealed class DashboardAddressTests
{
    /// <summary>
    /// Verifies that the browser gets the readable name, with the port of the local server.
    /// </summary>
    [TestMethod]
    public void ForBrowser_replaces_the_loopback_address_and_keeps_the_port()
    {
        DashboardAddress.ForBrowser("http://127.0.0.1:52731").Should().Be("http://deltaracing.localhost:52731");
    }

    /// <summary>
    /// Verifies that an address that is not the loopback one stays as it is.
    /// </summary>
    [TestMethod]
    public void ForBrowser_keeps_any_other_address()
    {
        DashboardAddress.ForBrowser("http://localhost:5000").Should().Be("http://localhost:5000");
    }
}
