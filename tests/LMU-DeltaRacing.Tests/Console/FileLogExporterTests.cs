using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LMU_DeltaRacing.Tests.Console;

public sealed class FileLogExporterTests
{
    [Fact]
    public void Export_overwrites_the_previous_run_and_writes_logs_to_one_file()
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
                options.AddProcessor(new SimpleLogRecordExportProcessor(new FileLogExporter(logPath)));
            })))
            {
                loggerFactory.CreateLogger("ImportTest").LogInformation("Current run {RunNumber}", 7);
            }

            var log = File.ReadAllText(logPath);

            log.Should().Contain("Current run 7");
            log.Should().Contain("ImportTest");
            log.Should().NotContain("previous run");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
