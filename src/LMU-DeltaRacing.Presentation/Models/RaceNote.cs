namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// The note written by the player about one session. Every field is optional.
/// </summary>
public sealed record RaceNote
{
    /// <summary>
    /// The lowest feeling mark.
    /// </summary>
    public const int MinimumFeeling = 1;

    /// <summary>
    /// The highest feeling mark.
    /// </summary>
    public const int MaximumFeeling = 5;

    /// <summary>
    /// Creates a note; texts are trimmed and a missing text becomes an empty string.
    /// </summary>
    /// <param name="sessionId">The session noted, by <c>Session.Id</c>, which is stable between launches.</param>
    /// <param name="conditions">The track conditions, when noted.</param>
    /// <param name="car">How the car behaved.</param>
    /// <param name="setup">The setup used, as free text.</param>
    /// <param name="feeling">The feeling, from <see cref="MinimumFeeling"/> to <see cref="MaximumFeeling"/>, when noted.</param>
    /// <param name="remarks">Any other remark.</param>
    public RaceNote(
        Guid sessionId,
        RaceConditions? conditions,
        string? car,
        string? setup,
        int? feeling,
        string? remarks)
    {
        if (conditions is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(conditions), conditions, "Unknown track conditions.");
        }

        if (feeling is < MinimumFeeling or > MaximumFeeling)
        {
            throw new ArgumentOutOfRangeException(nameof(feeling), feeling, "The feeling is a mark from 1 to 5.");
        }

        SessionId = sessionId;
        Conditions = conditions;
        Car = car?.Trim() ?? string.Empty;
        Setup = setup?.Trim() ?? string.Empty;
        Feeling = feeling;
        Remarks = remarks?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets the session noted.
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// Gets the track conditions, when noted.
    /// </summary>
    public RaceConditions? Conditions { get; }

    /// <summary>
    /// Gets how the car behaved, or an empty string.
    /// </summary>
    public string Car { get; }

    /// <summary>
    /// Gets the setup used, or an empty string.
    /// </summary>
    public string Setup { get; }

    /// <summary>
    /// Gets the feeling mark, when noted.
    /// </summary>
    public int? Feeling { get; }

    /// <summary>
    /// Gets any other remark, or an empty string.
    /// </summary>
    public string Remarks { get; }

    /// <summary>
    /// Gets whether no field is filled in: such a note is deleted rather than stored.
    /// </summary>
    public bool IsEmpty =>
        Conditions is null && Feeling is null && Car.Length == 0 && Setup.Length == 0 && Remarks.Length == 0;
}
