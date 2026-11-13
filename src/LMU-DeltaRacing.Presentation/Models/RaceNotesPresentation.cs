using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Presentation.Localization;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Formats the race notes of the player for Race details, My races and the session comparison.
/// </summary>
public static class RaceNotesPresentation
{
    /// <summary>
    /// The length the setup is cut at in a one-line summary.
    /// </summary>
    public const int SummaryTextLength = 60;

    private const string Separator = " · ";

    /// <summary>
    /// The conditions offered, in display order.
    /// </summary>
    public static IReadOnlyList<RaceConditions> Conditions { get; } =
        [RaceConditions.Dry, RaceConditions.Damp, RaceConditions.Wet, RaceConditions.Mixed];

    /// <summary>
    /// Returns whether the selected driver drove at least one lap on wet tyres in the session, the
    /// only hint of rain in the result files.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="userName">The selected driver.</param>
    public static bool UsedWetTyres(Session session, string? userName)
    {
        ArgumentNullException.ThrowIfNull(session);

        return DriverProfile.FindDriver(session, userName)?.Laps.Any(lap =>
            IsWet(lap.Telemetry.FrontCompound) || IsWet(lap.Telemetry.RearCompound)) == true;
    }

    /// <summary>
    /// Returns the emoji of the conditions.
    /// </summary>
    /// <param name="conditions">The conditions.</param>
    public static string Icon(RaceConditions conditions) =>
        conditions switch
        {
            RaceConditions.Dry => "☀️",
            RaceConditions.Damp => "🌥️",
            RaceConditions.Wet => "🌧️",
            _ => "🌦️"
        };

    /// <summary>
    /// Returns the name of the conditions in the interface language.
    /// </summary>
    /// <param name="conditions">The conditions.</param>
    public static string Label(RaceConditions conditions) =>
        conditions switch
        {
            RaceConditions.Dry => Texts.Get("RaceNotes.Dry"),
            RaceConditions.Damp => Texts.Get("RaceNotes.Damp"),
            RaceConditions.Wet => Texts.Get("RaceNotes.Wet"),
            _ => Texts.Get("RaceNotes.Mixed")
        };

    /// <summary>
    /// Returns the feeling as five stars, such as <c>★★★★☆</c>, or an empty string without feeling.
    /// </summary>
    /// <param name="feeling">The feeling mark.</param>
    public static string Stars(int? feeling) =>
        feeling is { } mark
            ? new string('★', mark) + new string('☆', RaceNote.MaximumFeeling - mark)
            : string.Empty;

    /// <summary>
    /// Returns the conditions and the feeling, such as <c>🌧️ Pluie · ★★★★☆</c>, or an empty string.
    /// </summary>
    /// <param name="note">The note.</param>
    public static string Heading(RaceNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return string.Join(
            Separator,
            new[]
            {
                note.Conditions is { } conditions ? $"{Icon(conditions)} {Label(conditions)}" : null,
                note.Feeling is null ? null : Stars(note.Feeling)
            }.OfType<string>());
    }

    /// <summary>
    /// Returns a one-line summary: conditions, feeling and the start of the setup. A note with none
    /// of them is summarized by the start of its first text.
    /// </summary>
    /// <param name="note">The note.</param>
    public static string Summary(RaceNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        var heading = Heading(note);
        if (note.Setup.Length > 0)
        {
            var setup = Labelled(Texts.Get("RaceNotes.Setup"), Cut(note.Setup));
            return heading.Length == 0 ? setup : $"{heading}{Separator}{setup}";
        }

        if (heading.Length > 0)
        {
            return heading;
        }

        return note.Car.Length > 0
            ? Labelled(Texts.Get("RaceNotes.Car"), Cut(note.Car))
            : Labelled(Texts.Get("RaceNotes.Remarks"), Cut(note.Remarks));
    }

    /// <summary>
    /// Returns the whole note, one filled field per line, for a tooltip.
    /// </summary>
    /// <param name="note">The note.</param>
    public static string FullText(RaceNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return string.Join(
            '\n',
            new[]
            {
                Heading(note),
                note.Car.Length == 0 ? string.Empty : Labelled(Texts.Get("RaceNotes.Car"), note.Car),
                note.Setup.Length == 0 ? string.Empty : Labelled(Texts.Get("RaceNotes.Setup"), note.Setup),
                note.Remarks.Length == 0 ? string.Empty : Labelled(Texts.Get("RaceNotes.Remarks"), note.Remarks)
            }.Where(line => line.Length > 0));
    }

    /// <summary>
    /// Returns the tooltip of a row of My races, the race first and then its qualifying, or
    /// <see langword="null"/> when neither session has a note.
    /// </summary>
    /// <param name="record">The row.</param>
    /// <param name="notes">The notes, by session.</param>
    public static string? MyRaceTooltip(MyRaceRecord record, IReadOnlyDictionary<Guid, RaceNote> notes)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(notes);

        var lines = new[]
        {
            Line(record.RaceSessionId, Texts.Get("Progression.Race")),
            Line(record.QualifyingSessionId, Texts.Get("Progression.Qualifying"))
        }.OfType<string>().ToArray();

        return lines.Length == 0 ? null : string.Join('\n', lines);

        string? Line(Guid? sessionId, string session) =>
            sessionId is { } id && notes.TryGetValue(id, out var note) ? Labelled(session, Summary(note)) : null;
    }

    private static string Labelled(string label, string text) => Texts.Format("RaceNotes.Labelled", label, text);

    private static string Cut(string text)
    {
        // A summary stays on one line: line breaks of a text become spaces.
        var line = string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return line.Length <= SummaryTextLength ? line : $"{line[..SummaryTextLength].TrimEnd()}…";
    }

    private static bool IsWet(string compound) => string.Equals(compound, "Wet", StringComparison.OrdinalIgnoreCase);
}
