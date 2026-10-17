using System.Text.Json;
using FluentAssertions;
using LMU_DeltaRacing.UI.Logging;
using Microsoft.Extensions.Logging;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies JSON Lines output for self-contained trimmed deployments.
/// </summary>
[TestClass]
public sealed class JsonFileLoggerProviderTests
{
    /// <summary>
    /// Verifies log records remain valid JSON without reflection-based serialization.
    /// </summary>
    [TestMethod]
    public void Logger_writes_one_valid_json_record_per_line()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lmu-json-logger-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var logPath = Path.Combine(directory, "application.log");

        try
        {
            using (var provider = new JsonFileLoggerProvider(logPath, LogLevel.Trace))
            {
                var logger = provider.CreateLogger("dashboard");
                logger.LogInformation("Imported session {SessionId}", "race-1");
                logger.LogError(new InvalidOperationException("sample failure"), "Import failed");
            }

            var lines = File.ReadAllLines(logPath);
            lines.Should().HaveCount(2);

            using var firstRecord = JsonDocument.Parse(lines[0]);
            firstRecord.RootElement.GetProperty("severity").GetString().Should().Be("Information");
            firstRecord.RootElement.GetProperty("category").GetString().Should().Be("dashboard");
            firstRecord.RootElement.GetProperty("message").GetString().Should().Contain("race-1");

            using var secondRecord = JsonDocument.Parse(lines[1]);
            secondRecord.RootElement.GetProperty("severity").GetString().Should().Be("Error");
            secondRecord.RootElement.GetProperty("exception").GetString().Should().Contain("sample failure");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
