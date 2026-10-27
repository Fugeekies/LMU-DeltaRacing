using Microsoft.Extensions.Options;

namespace LMU_DeltaRacing.ConsoleApp.Configuration;

/// <summary>
/// Validates the log file path and the configured file logging severity.
/// </summary>
public sealed class LmuApplicationOptionsValidator : IValidateOptions<LmuApplicationOptions>
{
    private readonly string baseDirectory;

    /// <summary>
    /// Creates a validator using the specified base directory for relative paths.
    /// </summary>
    /// <param name="baseDirectory">The executable directory used to resolve relative paths.</param>
    public LmuApplicationOptionsValidator(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        this.baseDirectory = Path.GetFullPath(baseDirectory);
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, LmuApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        try
        {
            var logFilePath = ResolveLogFilePath(options);
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (Directory.Exists(logFilePath))
            {
                failures.Add($"LogFilePath must refer to a file, not a directory: '{logFilePath}'.");
            }
            else if (logDirectory is null || !Directory.Exists(logDirectory))
            {
                failures.Add($"LogFilePath directory does not exist: '{logDirectory}'.");
            }
            else
            {
                ValidateWritableDirectory(logDirectory, failures);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            failures.Add($"LogFilePath is invalid: {exception.Message}");
        }

        ValidateWritableDirectory(baseDirectory, failures);

        if (!Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(
                options.FileMinimumLogLevel,
                ignoreCase: true,
                out var logLevel)
            || !Enum.IsDefined(logLevel))
        {
            failures.Add(
                $"FileMinimumLogLevel must be one of: {string.Join(", ", Enum.GetNames<Microsoft.Extensions.Logging.LogLevel>())}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Resolves the configured log file path relative to the executable directory.
    /// </summary>
    /// <param name="options">The application options.</param>
    /// <returns>An absolute log file path.</returns>
    public string ResolveLogFilePath(LmuApplicationOptions options) =>
        ResolvePath(options.LogFilePath);

    private string ResolvePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path));
    }

    private static void ValidateWritableDirectory(string directory, ICollection<string> failures)
    {
        var probePath = Path.Combine(directory, $".lmu-write-check-{Guid.NewGuid():N}.tmp");
        try
        {
            using var stream = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failures.Add($"LogFilePath directory is not writable: '{directory}'. {exception.Message}");
        }
        finally
        {
            if (File.Exists(probePath))
            {
                File.Delete(probePath);
            }
        }
    }
}
