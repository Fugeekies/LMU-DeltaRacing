namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// A qualifying session and the race that follows it, as driven by the selected driver.
/// Either side can be missing when a session could not be paired.
/// <c>ClassPosition</c> is the official class position in the race and <c>RaceResult</c> how the
/// race ended; both are <see langword="null"/> without race.
/// </summary>
public sealed record MyRaceRecord(
    string TrackVenue,
    string TrackCourse,
    string TrackEvent,
    DateTimeOffset StartedAt,
    string Category,
    string Car,
    TimeSpan? QualifyingTime,
    TimeSpan? QualifyingDelta,
    int? GridPosition,
    TimeSpan? RaceTime,
    TimeSpan? RaceDelta,
    int? LapsCompleted,
    TimeSpan? FinishTime,
    int? ClassPosition = null,
    HomeResultKind? RaceResult = null)
{
    /// <summary>
    /// Gets the short label of the race result: "3e", "DNF", "DQ", "NC", or "—" without race.
    /// </summary>
    public string ResultLabel => RaceResult switch
    {
        null => DisplayFormat.Missing,
        HomeResultKind.Win or HomeResultKind.Podium or HomeResultKind.Classified => DisplayFormat.Position(ClassPosition),
        { } kind => RaceResults.GetLabel(kind, ClassPosition)
    };

    /// <summary>
    /// Gets the highlight of the row. Only classified races are highlighted: a DNF, a DQ or an
    /// unclassified race keeps the default row even with a class position.
    /// </summary>
    public MyRacePositionTier PositionTier => RaceResult switch
    {
        HomeResultKind.Win or HomeResultKind.Podium or HomeResultKind.Classified => ClassPosition switch
        {
            1 => MyRacePositionTier.Gold,
            2 => MyRacePositionTier.Silver,
            3 => MyRacePositionTier.Bronze,
            4 or 5 => MyRacePositionTier.TopFive,
            >= 6 and <= 10 => MyRacePositionTier.TopTen,
            _ => MyRacePositionTier.None
        },
        _ => MyRacePositionTier.None
    };

    /// <summary>
    /// Gets whether the race ended on the class podium (classified first, second or third), which
    /// earns a trophy in the colour of the place.
    /// </summary>
    public bool IsPodium => PositionTier is MyRacePositionTier.Gold or MyRacePositionTier.Silver or MyRacePositionTier.Bronze;
}

/// <summary>
/// The highlight of a "my races" row, from the class position of the race.
/// </summary>
public enum MyRacePositionTier
{
    /// <summary>No highlight: no race, outside the top 10, DNF, DQ or not classified.</summary>
    None,

    /// <summary>First in class.</summary>
    Gold,

    /// <summary>Second in class.</summary>
    Silver,

    /// <summary>Third in class.</summary>
    Bronze,

    /// <summary>Fourth or fifth in class.</summary>
    TopFive,

    /// <summary>Sixth to tenth in class.</summary>
    TopTen
}
