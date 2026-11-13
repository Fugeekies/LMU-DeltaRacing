using FluentAssertions;
using LMU_DeltaRacing.Domain.Entities;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Presentation.Models;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies how the race notes are shown: rain suggestion, summaries and the tooltip of My races.
/// </summary>
[TestClass]
public sealed class RaceNotesPresentationTests
{
    private const string Tom = "Driver Tom";

    /// <summary>
    /// Verifies that wet tyres fitted by the selected driver suggest rain, and only them: the result
    /// files do not contain the weather.
    /// </summary>
    [TestMethod]
    [DataRow("2025_12_26_16_33_49-21R1.xml", Tom, true)]
    [DataRow("2025_08_14_19_57_19-69R1.xml", Tom, false)]
    [DataRow("2025_12_26_16_33_49-21R1.xml", "Nobody", false)]
    public void Wet_tyres_of_the_selected_driver_suggest_rain(string fileName, string userName, bool expected)
    {
        RaceNotesPresentation.UsedWetTyres(ParseFixture(fileName), userName).Should().Be(expected);
    }

    /// <summary>
    /// Verifies the one-line summary: conditions, feeling, then the setup cut at 60 characters.
    /// </summary>
    [TestMethod]
    public void A_summary_gives_the_conditions_the_feeling_and_the_start_of_the_setup()
    {
        var setup = new string('x', 70);
        var note = new RaceNote(Guid.NewGuid(), RaceConditions.Wet, "Understeer", setup, 4, null);

        RaceNotesPresentation.Summary(note).Should().Be($"🌧️ Pluie · ★★★★☆ · Réglages : {new string('x', 60)}…");
        RaceNotesPresentation.Summary(new RaceNote(Guid.NewGuid(), RaceConditions.Dry, null, null, 3, null))
            .Should().Be("☀️ Sec · ★★★☆☆");
    }

    /// <summary>
    /// Verifies that a note without conditions, feeling or setup is summarized by its first text.
    /// </summary>
    [TestMethod]
    public void A_note_with_only_a_remark_is_summarized_by_its_remark()
    {
        var note = new RaceNote(Guid.NewGuid(), null, null, null, null, "Pit too late");

        RaceNotesPresentation.Summary(note).Should().Be("Remarques : Pit too late");
    }

    /// <summary>
    /// Verifies the full text shown when hovering a note: every filled field, one per line.
    /// </summary>
    [TestMethod]
    public void The_full_text_lists_every_filled_field()
    {
        var note = new RaceNote(Guid.NewGuid(), RaceConditions.Mixed, "Oversteer", "Wing 6", null, "Late stop");

        RaceNotesPresentation.FullText(note).Should().Be(
            "🌦️ Variable\nVoiture : Oversteer\nRéglages : Wing 6\nRemarques : Late stop");
    }

    /// <summary>
    /// Verifies the tooltip of a row of My races, which pairs a race and its qualifying: the race
    /// first, then the qualifying, and nothing when neither has a note.
    /// </summary>
    [TestMethod]
    public void The_tooltip_of_my_races_lists_the_race_then_the_qualifying()
    {
        var qualifying = Guid.NewGuid();
        var race = Guid.NewGuid();
        var record = CreateRecord(qualifying, race);
        var notes = new Dictionary<Guid, RaceNote>
        {
            [qualifying] = new(qualifying, RaceConditions.Dry, null, null, 3, null),
            [race] = new(race, RaceConditions.Wet, null, null, 4, null)
        };

        RaceNotesPresentation.MyRaceTooltip(record, notes).Should().Be(
            "Course : 🌧️ Pluie · ★★★★☆\nQualification : ☀️ Sec · ★★★☆☆");
        RaceNotesPresentation.MyRaceTooltip(record, new Dictionary<Guid, RaceNote>()).Should().BeNull();
    }

    /// <summary>
    /// Verifies that My races knows which sessions a row pairs, so that it can find their notes.
    /// </summary>
    [TestMethod]
    public void My_races_keeps_the_qualifying_and_the_race_of_each_row()
    {
        var qualifying = ParseFixture("2026_10_02_21_13_54-84Q1.xml");
        var race = ParseFixture("2026_10_02_21_39_05-76R1.xml");

        var record = MyRacesPresentation.CreateRecords([qualifying, race], Tom).Should().ContainSingle().Which;

        record.QualifyingSessionId.Should().Be(qualifying.Id);
        record.RaceSessionId.Should().Be(race.Id);
    }

    private static MyRaceRecord CreateRecord(Guid qualifying, Guid race) =>
        new("Track", "Course", "Event", DateTimeOffset.UnixEpoch, "LMGT3", "Car", null, null, null, null, null, null, null)
        {
            QualifyingSessionId = qualifying,
            RaceSessionId = race
        };

    private static Session ParseFixture(string fileName)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "Results", fileName));
        return new XmlResultParser().Parse(stream);
    }
}
