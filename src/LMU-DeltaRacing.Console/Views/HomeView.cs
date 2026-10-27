using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// Renders the career dashboard of the selected driver, with the same statistics as the home page
/// of the dashboard. The breakdown by category is shown as table columns.
/// </summary>
public static class HomeView
{
    /// <summary>
    /// Creates the home view.
    /// </summary>
    /// <param name="dashboard">The career statistics of the driver.</param>
    /// <param name="userName">The selected driver.</param>
    public static IRenderable Create(HomeDashboard dashboard, string? userName)
    {
        ArgumentNullException.ThrowIfNull(dashboard);

        if (dashboard.IsEmpty)
        {
            return new Rows(
                new Markup("[bold]Aucune course disponible[/]"),
                new Markup(ConsoleTheme.MutedMarkup(
                    "Les sessions de qualification et de course du pilote sélectionné apparaîtront ici après leur import.")));
        }

        // Laps cover races and qualifying sessions, so every category driven gets a column.
        var categories = dashboard.Laps.Categories
            .Where(category => category.Value > 0)
            .Select(category => category.Category)
            .ToArray();

        return new Rows(
            new Columns(CreateDriverPanel(dashboard, userName), CreateFormPanel(dashboard)) { Expand = false },
            Section("Palmarès", CreateTable(
                categories,
                ("Courses", dashboard.Races, DisplayFormat.Count),
                ("Victoires", dashboard.Wins, DisplayFormat.Count),
                ("Podiums", dashboard.Podiums, DisplayFormat.Count),
                ("Top 5", dashboard.Top5, DisplayFormat.Count),
                ("Top 10", dashboard.Top10, DisplayFormat.Count),
                ("Position moyenne", dashboard.AveragePosition, DisplayFormat.Average))),
            Section("Roulage", CreateTable(
                categories,
                ("Tours bouclés", dashboard.Laps, DisplayFormat.Count),
                ("Distance", dashboard.DistanceKm, DisplayFormat.Distance),
                ("Temps en piste", dashboard.TimeOnTrack, DisplayFormat.Duration))),
            Section("Qualification & départ", CreateStartTable(dashboard, categories)),
            Section("Abandons", CreateTable(
                categories,
                ("Abandons", dashboard.Retirements, DisplayFormat.Count),
                ("Disqualifications", dashboard.Disqualifications, DisplayFormat.Count),
                ("Non classé", dashboard.Unclassified, DisplayFormat.Count))),
            Section("Causes d'abandon", CreateRetirementCauses(dashboard.RetirementCauses)));
    }

    private static Panel CreateDriverPanel(HomeDashboard dashboard, string? userName)
    {
        var grid = new Grid().AddColumn().AddColumn();
        AddFavorite(grid, "Circuit favori", dashboard.FavoriteCircuit);
        AddFavorite(grid, "Voiture favorite", dashboard.FavoriteCar);

        var lines = new List<IRenderable> { new Markup($"[bold]{Markup.Escape(userName ?? DisplayFormat.Missing)}[/]") };
        if (dashboard.FirstRaceAt is { } firstRaceAt && dashboard.LastRaceAt is { } lastRaceAt)
        {
            lines.Add(new Markup(ConsoleTheme.MutedMarkup(
                $"En piste du {DisplayFormat.Day(firstRaceAt)} au {DisplayFormat.Day(lastRaceAt)}")));
        }

        lines.Add(Text.Empty);
        lines.Add(grid);
        return new Panel(new Rows(lines)).Header("Pilote").RoundedBorder();
    }

    private static void AddFavorite(Grid grid, string label, HomeFavorite? favorite)
    {
        if (favorite is null)
        {
            grid.AddRow(new Markup(ConsoleTheme.MutedMarkup(label)), new Text(DisplayFormat.Missing));
            return;
        }

        var name = favorite.Detail is { } detail ? $"{favorite.Name} — {detail}" : favorite.Name;
        grid.AddRow(new Markup(ConsoleTheme.MutedMarkup(label)), new Markup($"[bold]{Markup.Escape(name)}[/]"));
        grid.AddRow(
            Text.Empty,
            new Markup(ConsoleTheme.MutedMarkup(
                $"{Plural(favorite.Races, "course", "courses")} · {Plural(favorite.Wins, "victoire", "victoires")} · "
                + Plural(favorite.Podiums, "podium", "podiums"))));
    }

    private static Panel CreateFormPanel(HomeDashboard dashboard)
    {
        var lines = new List<IRenderable>
        {
            dashboard.RecentForm.Count == 0
                ? new Text(DisplayFormat.Missing)
                : new Markup(string.Join("  ", dashboard.RecentForm.Select(FormatPill)))
        };

        if (dashboard.LastRace is { } lastRace)
        {
            lines.Add(Text.Empty);
            lines.Add(new Markup(ConsoleTheme.MutedMarkup("Dernière course")));
            lines.Add(new Markup(
                $"[bold]{DisplayFormat.DateTime(lastRace.StartedAt)}[/] · "
                + Markup.Escape($"{lastRace.TrackVenue} · {lastRace.Car}")));
            var position = lastRace.ClassPosition is { } classPosition
                ? $"P{classPosition} en classe"
                : "Position inconnue";
            lines.Add(new Markup(Markup.Escape(
                $"{position} · {lastRace.StatusLabel} · {DisplayFormat.Lap(lastRace.BestLap)}")));
        }

        return new Panel(new Rows(lines)).Header("Forme récente").RoundedBorder();
    }

    private static string FormatPill(HomeFormEntry entry) => entry.Kind switch
    {
        HomeResultKind.Win => $"[bold {ConsoleTheme.Win.ToMarkup()}]{Markup.Escape(entry.Label)}[/]",
        HomeResultKind.Podium => $"[bold {ConsoleTheme.Gain.ToMarkup()}]{Markup.Escape(entry.Label)}[/]",
        HomeResultKind.Retired or HomeResultKind.Disqualified => ConsoleTheme.Colored(entry.Label, ConsoleTheme.Loss),
        HomeResultKind.Unclassified => ConsoleTheme.MutedMarkup(entry.Label),
        _ => Markup.Escape(entry.Label)
    };

    private static Table CreateTable(
        IReadOnlyList<string> categories,
        params (string Label, HomeStat Stat, Func<double?, string> Format)[] rows)
    {
        var hasRate = rows.Any(row => row.Stat.Rate is not null);
        var table = CreateStatTable(categories, hasRate);
        foreach (var (label, stat, format) in rows)
        {
            var cells = new List<string> { Markup.Escape(label), Markup.Escape(format(stat.Value)) };
            if (hasRate)
            {
                cells.Add(stat.Rate is { } rate ? Markup.Escape(DisplayFormat.Percent(rate)) : string.Empty);
            }

            cells.AddRange(categories.Select(category => Markup.Escape(format(GetValue(stat, category)))));
            table.AddRow(cells.ToArray());
        }

        return table;
    }

    private static Table CreateStartTable(HomeDashboard dashboard, IReadOnlyList<string> categories)
    {
        var table = CreateTable(
            categories,
            ("Poles", dashboard.Poles, DisplayFormat.Count),
            ("Qualif moyenne", dashboard.AverageQualifyingPosition, DisplayFormat.Average));

        var gained = dashboard.AveragePositionsGained;
        var total = $"{SignedMarkup(gained.Value, "0.0")} / course";
        if (dashboard.TotalPositionsGained is { } totalGained)
        {
            total += $"{Environment.NewLine}{SignedMarkup(totalGained, "0")} au total";
        }

        var cells = new List<string> { "Places gagnées", total, string.Empty };
        cells.AddRange(categories.Select(category => SignedMarkup(GetValue(gained, category), "0.0")));
        table.AddRow(cells.ToArray());
        return table;
    }

    private static Table CreateStatTable(IReadOnlyList<string> categories, bool hasRate)
    {
        var table = new Table().Border(TableBorder.Square);
        table.AddColumn(string.Empty);
        table.AddColumn(new TableColumn("Total").RightAligned());
        if (hasRate)
        {
            table.AddColumn(new TableColumn("Taux").RightAligned());
        }

        foreach (var category in categories)
        {
            table.AddColumn(new TableColumn(ConsoleTheme.CategoryMarkup(category)).RightAligned());
        }

        return table;
    }

    private static IRenderable CreateRetirementCauses(IReadOnlyList<HomeShare> causes)
    {
        if (causes.Count == 0)
        {
            return new Text(DisplayFormat.Missing);
        }

        var chart = new BreakdownChart().Width(60).HideTags();
        var legend = new List<string>();
        for (var index = 0; index < causes.Count; index++)
        {
            var cause = causes[index];
            var color = ConsoleTheme.Causes[index % ConsoleTheme.Causes.Count];
            chart.AddItem(cause.Label, cause.Count, color);
            legend.Add($"{ConsoleTheme.Colored("■", color)} {Markup.Escape(cause.Label)} {cause.Count} "
                + ConsoleTheme.MutedMarkup($"({DisplayFormat.Percent(cause.Share)})"));
        }

        return new Rows(chart, new Markup(string.Join("  ", legend)));
    }

    private static Rows Section(string title, IRenderable content) =>
        new(Text.Empty, new Markup($" [bold]{Markup.Escape(title)}[/]"), content);

    private static double? GetValue(HomeStat stat, string category) =>
        stat.Categories.FirstOrDefault(part => part.Category == category)?.Value;

    private static string SignedMarkup(double? value, string format)
    {
        var text = DisplayFormat.Signed(value, format);
        return value switch
        {
            > 0 => ConsoleTheme.Colored(text, ConsoleTheme.Gain),
            < 0 => ConsoleTheme.Colored(text, ConsoleTheme.Loss),
            _ => Markup.Escape(text)
        };
    }

    private static string Plural(int count, string singular, string plural) =>
        $"{count} {(count > 1 ? plural : singular)}";
}
