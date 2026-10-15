using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Text.Json;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies the current-run OpenTelemetry file exporter.
/// </summary>
[TestClass]
public sealed class FileLogExporterTests
{
    /// <summary>
    /// Verifies that export replaces previous contents with one JSON object per log line.
    /// </summary>
    [TestMethod]
    public void Export_overwrites_previous_contents_and_writes_json_lines()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var logPath = Path.Combine(directory, "lmu-deltaracing.log");
        File.WriteAllText(logPath, "previous run");

        try
        {
            using (var loggerFactory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.ParseStateValues = true;
                options.AddProcessor(new SimpleLogRecordExportProcessor(new FileLogExporter(logPath)));
            })))
            {
                loggerFactory.CreateLogger("ImportTest").LogInformation("Current run {RunNumber}", 7);
            }

            File.Exists(logPath).Should().BeTrue("the latest log must remain available after shutdown");
            var logLines = File.ReadAllLines(logPath);
            logLines.Should().NotBeEmpty();
            logLines.Should().OnlyContain(line => !string.IsNullOrWhiteSpace(line));
            using var document = JsonDocument.Parse(logLines[0]);
            var record = document.RootElement;

            record.GetProperty("severity").GetString().Should().Be("Information");
            record.GetProperty("category").GetString().Should().Be("ImportTest");
            record.GetProperty("message").GetString().Should().Be("Current run 7");
            record.GetProperty("attributes")
                .EnumerateArray()
                .Select(attribute => attribute.GetProperty("key").GetString())
                .Should()
                .Contain("RunNumber");
            logLines.Should().NotContain(line => line.Contains("previous run", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
