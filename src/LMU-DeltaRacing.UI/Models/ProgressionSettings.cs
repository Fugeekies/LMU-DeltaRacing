using Microsoft.Extensions.Configuration;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// The length of the periods in which best laps are grouped.
/// </summary>
public enum ProgressionGranularity
{
    /// <summary>One calendar day.</summary>
    Day,

    /// <summary>One ISO week, Monday to Sunday.</summary>
    Week,

    /// <summary>One calendar month.</summary>
    Month,

    /// <summary>One calendar year.</summary>
    Year
}

/// <summary>
/// The value of a period that the chart line goes through.
/// </summary>
public enum ProgressionAggregate
{
    /// <summary>The fastest best lap of the period.</summary>
    Min,

    /// <summary>The average of the period's best laps.</summary>
    Average,

    /// <summary>The slowest best lap of the period.</summary>
    Max
}

/// <summary>
/// How sessions are split into chart series.
/// </summary>
public enum ProgressionMode
{
    /// <summary>One qualifying series and one race series, all cars together.</summary>
    MixedCars,

    /// <summary>One series per car, for a single session type.</summary>
    PerCar
}

/// <summary>
/// The session type drawn in <see cref="ProgressionMode.PerCar"/> mode.
/// </summary>
public enum ProgressionSession
{
    /// <summary>Qualifying sessions.</summary>
    Qualifying,

    /// <summary>Races.</summary>
    Race
}

/// <summary>
/// The chart settings of the progression page, saved in the <c>LMU:Progression</c> section.
/// </summary>
/// <param name="Granularity">The period length.</param>
/// <param name="Aggregate">The plotted value of each period.</param>
/// <param name="Mode">How sessions are split into series.</param>
/// <param name="Session">The session type drawn in per-car mode.</param>
public sealed record ProgressionSettings(
    ProgressionGranularity Granularity,
    ProgressionAggregate Aggregate,
    ProgressionMode Mode,
    ProgressionSession Session)
{
    /// <summary>
    /// The configuration section of the progression settings.
    /// </summary>
    public const string SectionName = "LMU:Progression";

    /// <summary>
    /// Gets the settings used when nothing is saved: monthly averages, all cars together, races in per-car mode.
    /// </summary>
    public static ProgressionSettings Default { get; } = new(
        ProgressionGranularity.Month,
        ProgressionAggregate.Average,
        ProgressionMode.MixedCars,
        ProgressionSession.Race);

    /// <summary>
    /// Reads the saved settings; a missing or unknown value falls back to its default.
    /// </summary>
    public static ProgressionSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionName);

        return new ProgressionSettings(
            Parse(section["Granularity"], Default.Granularity),
            Parse(section["Aggregate"], Default.Aggregate),
            Parse(section["Mode"], Default.Mode),
            Parse(section["Session"], Default.Session));
    }

    private static T Parse<T>(string? value, T fallback)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result) ? result : fallback;
}
