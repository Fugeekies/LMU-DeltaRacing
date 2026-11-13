using FluentAssertions;
using LMU_DeltaRacing.Presentation.Models;
using LMU_DeltaRacing.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Tests.Presentation;

/// <summary>
/// Verifies the persistence of the race notes in <c>race-notes.json</c>.
/// </summary>
[TestClass]
public sealed class RaceNotesStoreTests
{
    private string directory = string.Empty;
    private string notesPath = string.Empty;

    /// <summary>
    /// Creates an empty folder for the notes file of each test.
    /// </summary>
    [TestInitialize]
    public void CreateDirectory()
    {
        directory = Path.Combine(Path.GetTempPath(), $"lmu-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        notesPath = Path.Combine(directory, "race-notes.json");
    }

    /// <summary>
    /// Deletes the folder of the test.
    /// </summary>
    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(directory, recursive: true);

    /// <summary>
    /// Verifies that a missing file means no notes yet, without creating the file.
    /// </summary>
    [TestMethod]
    public void A_missing_file_has_no_notes()
    {
        var store = CreateStore();

        store.Find(Guid.NewGuid()).Should().BeNull();
        store.GetAll().Should().BeEmpty();
        File.Exists(notesPath).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a saved note is read back by the next launch of the application.
    /// </summary>
    [TestMethod]
    public void A_saved_note_is_read_back_after_a_restart()
    {
        var sessionId = Guid.NewGuid();
        var note = new RaceNote(sessionId, RaceConditions.Wet, "Understeer on entry", "Wing 6, pressures -0.2", 4, "Pit too late");

        CreateStore().Save(note);

        var reloaded = CreateStore().Find(sessionId);
        reloaded.Should().Be(note);
        Directory.GetFiles(directory, "*.tmp").Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that each session keeps its own note and that saving again replaces it.
    /// </summary>
    [TestMethod]
    public void Saving_a_note_again_replaces_only_the_note_of_that_session()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var store = CreateStore();
        store.Save(new RaceNote(first, RaceConditions.Dry, null, null, 3, null));
        store.Save(new RaceNote(second, null, null, "Base setup", null, null));

        store.Save(new RaceNote(first, RaceConditions.Mixed, null, null, 5, null));

        var reloaded = CreateStore();
        reloaded.GetAll().Keys.Should().BeEquivalentTo([first, second]);
        reloaded.Find(first)!.Conditions.Should().Be(RaceConditions.Mixed);
        reloaded.Find(first)!.Feeling.Should().Be(5);
        reloaded.Find(second)!.Setup.Should().Be("Base setup");
    }

    /// <summary>
    /// Verifies that clearing every field deletes the note instead of keeping an empty one.
    /// </summary>
    [TestMethod]
    public void Saving_an_empty_note_deletes_it()
    {
        var sessionId = Guid.NewGuid();
        var store = CreateStore();
        store.Save(new RaceNote(sessionId, RaceConditions.Dry, null, null, null, null));

        store.Save(new RaceNote(sessionId, null, "  ", "", null, null));

        store.Find(sessionId).Should().BeNull();
        CreateStore().GetAll().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an unreadable file is never overwritten: the notes it holds are not lost.
    /// </summary>
    [TestMethod]
    public void An_unreadable_file_is_kept_and_saving_is_refused()
    {
        const string content = "{ not json";
        File.WriteAllText(notesPath, content);
        var store = CreateStore();

        store.Find(Guid.NewGuid()).Should().BeNull();
        store.IsUnreadable.Should().BeTrue();
        var save = () => store.Save(new RaceNote(Guid.NewGuid(), RaceConditions.Dry, null, null, null, null));

        save.Should().Throw<InvalidDataException>();
        File.ReadAllText(notesPath).Should().Be(content);
    }

    /// <summary>
    /// Verifies that the feeling is a mark from 1 to 5 and that the texts are trimmed.
    /// </summary>
    [TestMethod]
    public void A_note_checks_the_feeling_and_trims_its_texts()
    {
        var create = () => new RaceNote(Guid.NewGuid(), null, null, null, 6, null);
        create.Should().Throw<ArgumentOutOfRangeException>();

        var note = new RaceNote(Guid.NewGuid(), null, "  Oversteer  ", null, 1, null);
        note.Car.Should().Be("Oversteer");
        note.Setup.Should().BeEmpty();
        note.IsEmpty.Should().BeFalse();
    }

    private RaceNotesStore CreateStore() => new(notesPath, NullLogger<RaceNotesStore>.Instance);
}
