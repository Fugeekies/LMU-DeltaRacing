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
    /// Verifies that export replaces previous contents with readable structured JSON.
    /// </summary>
    [TestMethod]
    public void Export_overwrites_the_previous_run_and_writes_indented_structured_json()
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

            var log = File.ReadAllText(logPath);
            using var document = JsonDocument.Parse(log);
            var record = document.RootElement[0];

            document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            record.GetProperty("severity").GetString().Should().Be("Information");
            record.GetProperty("category").GetString().Should().Be("ImportTest");
            record.GetProperty("message").GetString().Should().Be("Current run 7");
            record.GetProperty("attributes")
                .EnumerateArray()
                .Select(attribute => attribute.GetProperty("key").GetString())
                .Should()
                .Contain("RunNumber");
            log.Should().Contain($"{Environment.NewLine}    \"timestamp\"");
            log.Should().NotContain("previous run");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
