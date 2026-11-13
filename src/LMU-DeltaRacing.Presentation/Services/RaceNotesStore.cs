using System.Text.Json;
using System.Text.Json.Serialization;
using LMU_DeltaRacing.Presentation.Models;
using Microsoft.Extensions.Logging;

namespace LMU_DeltaRacing.Presentation.Services;

/// <summary>
/// Reads and writes the race notes of the player in <c>race-notes.json</c>, next to
/// <c>appsettings.json</c>. The notes are the only data typed by the player: unlike the sessions,
/// which are read again from the result files at each launch, they must survive a restart.
/// </summary>
public sealed class RaceNotesStore
{
    /// <summary>
    /// The name of the notes file.
    /// </summary>
    public const string FileName = "race-notes.json";

    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;
    private readonly ILogger<RaceNotesStore> logger;
    private readonly Lock gate = new();
    private Dictionary<Guid, RaceNote>? notes;
    private bool isUnreadable;

    /// <summary>
    /// Creates a store for the given notes file; the file is read at the first access.
    /// </summary>
    /// <param name="filePath">The path of the notes file, which may not exist yet.</param>
    /// <param name="logger">The logger used to report a notes file that cannot be read.</param>
    public RaceNotesStore(string filePath, ILogger<RaceNotesStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(logger);

        this.filePath = Path.GetFullPath(filePath);
        this.logger = logger;
    }

    /// <summary>
    /// Raised after a note is saved or deleted, outside the lock, on the thread that saved it.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets whether the notes file exists but cannot be read. Its notes are then unavailable, and
    /// saving is refused so that the file is never overwritten.
    /// </summary>
    public bool IsUnreadable
    {
        get
        {
            lock (gate)
            {
                EnsureLoaded();
                return isUnreadable;
            }
        }
    }

    /// <summary>
    /// Returns the note of a session, or <see langword="null"/> when the session has none.
    /// </summary>
    /// <param name="sessionId">The session, by <c>Session.Id</c>.</param>
    public RaceNote? Find(Guid sessionId)
    {
        lock (gate)
        {
            return EnsureLoaded().GetValueOrDefault(sessionId);
        }
    }

    /// <summary>
    /// Returns every note, by session.
    /// </summary>
    public IReadOnlyDictionary<Guid, RaceNote> GetAll()
    {
        lock (gate)
        {
            return new Dictionary<Guid, RaceNote>(EnsureLoaded());
        }
    }

    /// <summary>
    /// Saves the note of a session, replacing the previous one; an empty note deletes it.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <exception cref="InvalidDataException">The notes file exists but cannot be read.</exception>
    public void Save(RaceNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        lock (gate)
        {
            var current = EnsureLoaded();
            if (isUnreadable)
            {
                throw new InvalidDataException($"The notes file {FileName} cannot be read; it is left unchanged.");
            }

            var updated = new Dictionary<Guid, RaceNote>(current);
            if (note.IsEmpty)
            {
                updated.Remove(note.SessionId);
            }
            else
            {
                updated[note.SessionId] = note;
            }

            Write(updated.Values);
            notes = updated;
        }

        Changed?.Invoke();
    }

    private Dictionary<Guid, RaceNote> EnsureLoaded()
    {
        if (notes is not null)
        {
            return notes;
        }

        notes = [];
        if (!File.Exists(filePath))
        {
            return notes;
        }

        try
        {
            var file = JsonSerializer.Deserialize<NotesFile>(File.ReadAllText(filePath), JsonOptions)
                ?? throw new InvalidDataException("The notes file is empty.");
            foreach (var entry in file.Notes ?? [])
            {
                var note = new RaceNote(entry.SessionId, entry.Conditions, entry.Car, entry.Setup, entry.Feeling, entry.Remarks);
                if (!note.IsEmpty)
                {
                    notes[note.SessionId] = note;
                }
            }

            logger.LogInformation("Loaded {NoteCount} race notes from {SourceFile}", notes.Count, FileName);
        }
        catch (Exception exception) when (
            exception is JsonException or InvalidDataException or ArgumentException or IOException
                or UnauthorizedAccessException or NotSupportedException)
        {
            // Keeping the file untouched protects notes typed by hand or by a newer version.
            notes = [];
            isUnreadable = true;
            logger.LogError(exception, "Race notes file {SourceFile} cannot be read; notes are unavailable", FileName);
        }

        return notes;
    }

    private void Write(IEnumerable<RaceNote> values)
    {
        var file = new NotesFile
        {
            Version = CurrentVersion,
            // Sorted so that the file stays the same when the notes do not change.
            Notes = values
                .OrderBy(note => note.SessionId)
                .Select(note => new NoteEntry
                {
                    SessionId = note.SessionId,
                    Conditions = note.Conditions,
                    Car = NullIfEmpty(note.Car),
                    Setup = NullIfEmpty(note.Setup),
                    Feeling = note.Feeling,
                    Remarks = NullIfEmpty(note.Remarks)
                })
                .ToList()
        };

        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(file, JsonOptions));
            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        logger.LogInformation("Saved {NoteCount} race notes to {SourceFile}", file.Notes.Count, FileName);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private sealed class NotesFile
    {
        public int Version { get; set; }

        public List<NoteEntry>? Notes { get; set; }
    }

    private sealed class NoteEntry
    {
        public Guid SessionId { get; set; }

        public RaceConditions? Conditions { get; set; }

        public string? Car { get; set; }

        public string? Setup { get; set; }

        public int? Feeling { get; set; }

        public string? Remarks { get; set; }
    }
}
