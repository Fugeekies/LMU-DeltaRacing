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
    /// <param name="sessions">The imported sessions.</param>
    /// <param name="userName">The selected driver.</param>
    /// <param name="ratings">The driver ratings, read for the current rating of each opponent.</param>
    public static IReadOnlyList<DriverRivalry> CreateRivalries(
        IEnumerable<Session> sessions,
        string? userName,
        DriverRatingHistory ratings)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(ratings);
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

            // Without a log, the race says nothing about contacts: it must not count as zero.
            var playerContacts = session.Events.Count == 0
                ? null
                : DrivingConductPresentation.CreateIncidents(session)
                    .GetValueOrDefault(player.Name.Trim(), [])
                    .Where(incident => incident.Kind == SessionEventKind.VehicleContact && incident.Detail is not null)
                    .Select(incident => incident.Detail!.Trim())
                    .ToArray();

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
                rival.GapSum += opponentRank - playerRank;
                if (playerRank < opponentRank)
                {
                    rival.Wins++;
                }
                else if (playerRank > opponentRank)
                {
                    rival.Losses++;
                }

                if (rival.LastFacedTrack is null || session.StartedAt > rival.LastFacedAt)
                {
                    rival.LastFacedAt = session.StartedAt;
                    rival.LastFacedTrack = session.TrackVenue;
                }

                if (playerContacts is not null)
                {
                    rival.Contacts = (rival.Contacts ?? 0) + playerContacts.Count(name =>
                        string.Equals(name, opponent.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        return totals.Values
            .Select(rival => new DriverRivalry(
                rival.Name,
                rival.SessionsFaced,
                rival.Wins,
                rival.Losses,
                (double)rival.GapSum / rival.SessionsFaced,
                rival.LastFacedAt,
                rival.LastFacedTrack!,
                ratings.FindDriver(rival.Name)?.Rating,
                rival.Contacts))
            .ToArray();
    }

    private sealed class RivalryTotals(string name)
    {
        public string Name { get; } = name;
        public int SessionsFaced { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int GapSum { get; set; }
        public DateTimeOffset LastFacedAt { get; set; }
        public string? LastFacedTrack { get; set; }
        public int? Contacts { get; set; }
    }
}
