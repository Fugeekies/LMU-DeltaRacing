using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.UI.Models;

/// <summary>
/// Aggregates head-to-head results from imported sessions.
/// </summary>
public static class DriversRankingPresentation
{
    /// <summary>
    /// Creates one record per opponent with a classified result against the player.
    /// </summary>
    public static IReadOnlyList<DriverRivalry> CreateRivalries(IEnumerable<Session> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var totals = new Dictionary<string, RivalryTotals>(StringComparer.OrdinalIgnoreCase);
        foreach (var session in sessions)
        {
            var player = session.Drivers.FirstOrDefault(driver => driver.IsPlayer);
            if (player?.Position is not { } playerPosition)
            {
                continue;
            }

            foreach (var opponent in session.Drivers
                .Where(driver => !driver.IsPlayer
                    && !string.Equals(driver.Name, player.Name, StringComparison.OrdinalIgnoreCase)
                    && driver.Position.HasValue)
                .GroupBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()))
            {
                if (!totals.TryGetValue(opponent.Name, out var rival))
                {
                    rival = new RivalryTotals(opponent.Name);
                    totals.Add(opponent.Name, rival);
                }

                rival.SessionsFaced++;
                if (playerPosition < opponent.Position)
                {
                    rival.Wins++;
                }
                else if (playerPosition > opponent.Position)
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
