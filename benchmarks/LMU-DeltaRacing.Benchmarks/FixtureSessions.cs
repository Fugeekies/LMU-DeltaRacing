using System.Reflection;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;

namespace LMU_DeltaRacing.Benchmarks;

/// <summary>
/// The anonymized LMU result files of <c>assets/Results</c>, used to measure realistic histories.
/// </summary>
internal static class FixtureSessions
{
    /// <summary>
    /// The main driver of the fixtures.
    /// </summary>
    public const string UserName = "Driver Tom";

    /// <summary>
    /// Gets the directory of the real result files.
    /// </summary>
    public static string ResultsDirectory { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "ResultsDirectory")
            .Value!;

    /// <summary>
    /// Gets the paths of the result files, sorted by name.
    /// </summary>
    public static IReadOnlyList<string> Files { get; } =
        Directory.EnumerateFiles(ResultsDirectory, "*.xml").Order(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>
    /// Parses every result file, practice sessions included.
    /// </summary>
    public static IReadOnlyList<Session> LoadAll()
    {
        var parser = new XmlResultParser();
        return Files.Select(file =>
        {
            using var stream = File.OpenRead(file);
            return parser.Parse(stream);
        }).ToArray();
    }

    /// <summary>
    /// Repeats a history <paramref name="factor"/> times, each copy shifted after the previous one,
    /// so that a longer career keeps the circuits, classes and opponents of the real files.
    /// </summary>
    public static IReadOnlyList<Session> Scale(IReadOnlyList<Session> sessions, int factor)
    {
        if (factor == 1)
        {
            return sessions;
        }

        var span = sessions.Max(session => session.StartedAt) - sessions.Min(session => session.StartedAt) + TimeSpan.FromDays(1);
        return Enumerable.Range(0, factor)
            .SelectMany(copy => sessions.Select(session => copy == 0
                ? session
                : new Session(
                    Guid.NewGuid(),
                    session.StartedAt + span * copy,
                    session.TrackVenue,
                    session.TrackCourse,
                    session.TrackEvent,
                    session.SessionType,
                    session.Drivers,
                    session.MostLapsCompleted,
                    session.TrackLength)))
            .ToArray();
    }
}
