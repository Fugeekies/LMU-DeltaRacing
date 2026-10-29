using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Presentation.Models;

/// <summary>
/// Aggregates head-to-head results from imported sessions.
/// </summary>
public static class DriversRankingPresentation
{
    /// <summary>
    /// Creates one record per opponent met in a race, in the same vehicle class as the player.
    /// </summary>
    /// <remarks>
    /// Qualifying sessions are ignored because the view counts races. In multiclass races, only
    /// drivers of the player's class are real opponents: beating a slower-class car says nothing.
    /// </remarks>
    public static IReadOnlyList<DriverRivalry> CreateRivalries(
        IEnumerable<Session> sessions,
        string? userName)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return [];
        }

        var totals = new Dictionary<string, RivalryTotals>(StringComparer.OrdinalIgnoreCase);
        foreach (var session in sessions.Where(session => session.Kind == SessionKind.Race))
        {
            var player = DriverProfile.FindDriver(session, userName);
            if (player is null || player.ClassRank() is not { } playerRank)
            {
                continue;
            }

            foreach (var opponent in session.Drivers
                .Where(driver => !string.Equals(
                        driver.Name.Trim(),
                        player.Name.Trim(),
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        driver.CarClass.Trim(),
                        player.CarClass.Trim(),
                        StringComparison.OrdinalIgnoreCase)
                    && driver.ClassRank().HasValue)
                .GroupBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()))
            {
                if (!totals.TryGetValue(opponent.Name, out var rival))
                {
                    rival = new RivalryTotals(opponent.Name);
                    totals.Add(opponent.Name, rival);
                }

                rival.SessionsFaced++;
                var opponentRank = opponent.ClassRank()!.Value;
                if (playerRank < opponentRank)
                {
                    rival.Wins++;
                }
                else if (playerRank > opponentRank)
                {
                    rival.Losses++;
                }
            }
        }

        return totals.Values
            .Select(rival => new DriverRivalry(
                rival.Name,
                rival.SessionsFaced,
                rival.Wins,
                rival.Losses))
            .ToArray();
    }

    private sealed class RivalryTotals(string name)
    {
        public string Name { get; } = name;
        public int SessionsFaced { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }
}
