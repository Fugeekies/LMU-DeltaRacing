using System.Net.Http.Headers;
using System.Text.Json;
using LMU_DeltaRacing.UI.Models;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Asks GitHub once at startup for the latest release. This is the only request the application
/// sends to the Internet: it carries no result and no driver name.
/// </summary>
public sealed class UpdateCheckService : BackgroundService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/Fugeekies/LMU-DeltaRacing/releases/latest";

    // The check must never be noticed: a slow network only delays the information.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient httpClient;
    private readonly string currentVersion;
    private readonly Func<bool> isEnabled;
    private readonly ILogger<UpdateCheckService> logger;
    private volatile UpdateCheckResult result = new(UpdateCheckStatus.Pending, null);
    private int startupNoticeConsumed;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="httpClient">The client used to call GitHub.</param>
    /// <param name="currentVersion">The running version.</param>
    /// <param name="isEnabled">Reads the option that allows the check.</param>
    /// <param name="logger">The logger.</param>
    public UpdateCheckService(
        HttpClient httpClient,
        string currentVersion,
        Func<bool> isEnabled,
        ILogger<UpdateCheckService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(isEnabled);
        ArgumentNullException.ThrowIfNull(logger);

        this.httpClient = httpClient;
        this.currentVersion = currentVersion;
        this.isEnabled = isEnabled;
        this.logger = logger;
    }

    /// <summary>
    /// Raised when the check has finished.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the current state of the check.
    /// </summary>
    public UpdateCheckResult Result => result;

    /// <summary>
    /// Runs the check.
    /// </summary>
    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (!isEnabled())
        {
            Publish(new UpdateCheckResult(UpdateCheckStatus.Disabled, null));
            return;
        }

        if (!ApplicationVersion.TryParseRelease(currentVersion, out var current))
        {
            Publish(new UpdateCheckResult(UpdateCheckStatus.DevelopmentBuild, null));
            return;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);

            var latestTag = await ReadLatestTagAsync(timeout.Token).ConfigureAwait(false);
            if (!ApplicationVersion.TryParseRelease(latestTag, out var latest))
            {
                logger.LogWarning("The latest release has an unexpected tag: {ReleaseTag}", latestTag);
                Publish(new UpdateCheckResult(UpdateCheckStatus.Failed, null));
                return;
            }

            var latestVersion = latest!.ToString();
            if (latest > current)
            {
                logger.LogInformation(
                    "Version {LatestVersion} is available (running {CurrentVersion})",
                    latestVersion,
                    currentVersion);
                Publish(new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, latestVersion));
            }
            else
            {
                Publish(new UpdateCheckResult(UpdateCheckStatus.UpToDate, latestVersion));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The application is closing: nobody is left to read the result.
        }
        catch (Exception exception) when (
            exception is HttpRequestException or OperationCanceledException or JsonException or IOException)
        {
            logger.LogWarning(exception, "Could not check for a new version");
            Publish(new UpdateCheckResult(UpdateCheckStatus.Failed, null));
        }
    }

    /// <summary>
    /// Returns true once, when a newer release is known: the popup appears at startup only, not
    /// each time the page is reloaded.
    /// </summary>
    public bool TryConsumeStartupNotice() =>
        result.Status == UpdateCheckStatus.UpdateAvailable
        && Interlocked.Exchange(ref startupNoticeConsumed, 1) == 0;

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => CheckAsync(stoppingToken);

    private async Task<string?> ReadLatestTagAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);

        // GitHub rejects the requests that have no User-Agent.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LMU-DeltaRacing", currentVersion));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("tag_name", out var tag)
            && tag.ValueKind == JsonValueKind.String
                ? tag.GetString()
                : null;
    }

    private void Publish(UpdateCheckResult newResult)
    {
        result = newResult;
        Changed?.Invoke();
    }
}
