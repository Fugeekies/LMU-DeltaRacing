namespace LMU_DeltaRacing.Domain.Entities;

/// <summary>
/// Represents the elapsed time for one sector of a lap.
/// </summary>
/// <param name="Number">The one-based sector number.</param>
/// <param name="Time">The elapsed time for the sector.</param>
public sealed record Sector(int Number, TimeSpan Time);
