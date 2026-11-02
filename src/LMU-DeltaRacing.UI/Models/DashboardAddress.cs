namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Gives the local dashboard a readable address in the browser.
/// </summary>
/// <remarks>
/// Browsers resolve every <c>*.localhost</c> name to this machine, without any setting or
/// administrator right, so the name changes nothing to the server: it still only listens on
/// 127.0.0.1. The port stays visible: it is free when the application starts, and hiding it would
/// mean port 80, which needs administrator rights and is often taken.
/// </remarks>
public static class DashboardAddress
{
    private const string LoopbackPrefix = "http://127.0.0.1:";
    private const string BrowserPrefix = "http://deltaracing.localhost:";

    /// <summary>
    /// Returns the address to open in the browser for an address of the local server.
    /// </summary>
    /// <param name="serverAddress">The address the server listens on.</param>
    public static string ForBrowser(string serverAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);

        return serverAddress.StartsWith(LoopbackPrefix, StringComparison.Ordinal)
            ? string.Concat(BrowserPrefix, serverAddress.AsSpan(LoopbackPrefix.Length))
            : serverAddress;
    }
}
