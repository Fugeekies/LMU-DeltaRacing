using FluentAssertions;
using LMU_DeltaRacing.ConsoleApp;
using LMU_DeltaRacing.ConsoleApp.Configuration;

namespace LMU_DeltaRacing.Tests.Console;

/// <summary>
/// Verifies validation and resolution of application configuration paths.
/// </summary>
[TestClass]
public sealed class LmuApplicationOptionsValidatorTests
{
    /// <summary>
    /// Verifies that default settings resolve to the repository XML fixtures and executable directory.
    /// </summary>
    [TestMethod]
    public void Validate_accepts_defaults_and_resolves_the_repository_assets_directory()
    {
        var validator = new LmuApplicationOptionsValidator(AppContext.BaseDirectory);

        var result = validator.Validate(
            Microsoft.Extensions.Options.Options.DefaultName,
            new LmuApplicationOptions());

        result.Succeeded.Should().BeTrue();
        validator.ResolveResultsDirectory(new LmuApplicationOptions())
            .Should()
            .Be(XmlFileDiscovery.FindAssetsResultsDirectory(AppContext.BaseDirectory));
        validator.ResolveLogFilePath(new LmuApplicationOptions())
            .Should()
            .Be(Path.Combine(AppContext.BaseDirectory, "lmu-deltaracing.log"));
    }

    /// <summary>
    /// Verifies that a configured missing results directory is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_rejects_a_missing_results_directory()
    {
        var validator = new LmuApplicationOptionsValidator(AppContext.BaseDirectory);
        var options = new LmuApplicationOptions
        {
            ResultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        };

        var result = validator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle(message => message.Contains("ResultsDirectory", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that unsupported minimum log levels are rejected.
    /// </summary>
    [TestMethod]
    public void Validate_rejects_an_unknown_file_log_level()
    {
        var validator = new LmuApplicationOptionsValidator(AppContext.BaseDirectory);
        var options = new LmuApplicationOptions { FileMinimumLogLevel = "Verbose" };

        var result = validator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle(message => message.Contains("FileMinimumLogLevel", StringComparison.Ordinal));
    }
}
