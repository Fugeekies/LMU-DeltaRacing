using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// Renders the qualifying + race rows of the selected driver, one table per circuit, as on the
/// "my races" page of the dashboard.
/// </summary>
public static class MyRacesView
{
    /// <summary>
    /// The label of the empty circuit filter.
    /// </summary>
    public const string AllTracks = "Tous les circuits";

    /// <summary>
    /// The label of the empty category filter.
    /// </summary>
    public const string AllCategories = "Toutes les catégories";

    /// <summary>
    /// Creates the view of the records matching the filters.
    /// </summary>
    /// <param name="records">Every record of the driver.</param>
    /// <param name="trackVenue">The selected venue, or <see langword="null"/> for all circuits.</param>
    /// <param name="category">The selected category, or <see langword="null"/> for all categories.</param>
    public static IRenderable Create(IReadOnlyList<MyRaceRecord> records, string? trackVenue, string? category)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return new Rows(
                new Markup("[bold]Aucune course disponible[/]"),
                new Markup(ConsoleTheme.MutedMarkup(
                    "Les sessions de qualification et de course du pilote sélectionné apparaîtront ici après leur import.")));
        }

        var lines = new List<IRenderable>
        {
            new Markup(ConsoleTheme.MutedMarkup(
                $" Circuit : {trackVenue ?? AllTracks} · Catégorie : {category ?? AllCategories}"))
        };

        var filtered = MyRacesFilters.Apply(records, trackVenue, category);
        if (filtered.Count == 0)
        {
            lines.Add(Text.Empty);
            lines.Add(new Markup("Aucune course ne correspond à ces filtres."));
            return new Rows(lines);
        }

        foreach (var circuit in filtered
                     .GroupBy(record => (record.TrackVenue, record.TrackCourse))
                     .OrderBy(group => group.Key.TrackVenue))
        {
            var name = string.Equals(circuit.Key.TrackVenue, circuit.Key.TrackCourse, StringComparison.OrdinalIgnoreCase)
                ? circuit.Key.TrackVenue
                : $"{circuit.Key.TrackVenue} — {circuit.Key.TrackCourse}";
            var count = circuit.Count();
            lines.Add(Text.Empty);
            lines.Add(new Rule(
                    $"[bold]{Markup.Escape(name)}[/] ─ {count} {(count > 1 ? "résultats" : "résultat")}")
                .LeftJustified());
            lines.Add(CreateTable(MyRacesFilters.OrderRows(circuit)));
        }

        return new Rows(lines);
    }

    private static Table CreateTable(IEnumerable<MyRaceRecord> records)
    {
        // In 120 columns, only the car name may wrap: dates and times stay on one line.
        var table = new Table().Border(TableBorder.Square);
        table.AddColumn(new TableColumn("CAT.").NoWrap());
        table.AddColumn(new TableColumn("DATE").NoWrap());
        table.AddColumn("VOITURE");
        table.AddColumn(new TableColumn("CLASS.").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("GRILLE").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("COURSE").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("Δ COURSE").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("TOURS").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("TEMPS TOTAL").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("QUALIF").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("Δ QUALIF").RightAligned().NoWrap());

        // The empty row stands for the separators of the dashboard, which carry the link to its
        // progression page: the console has no such page.
        string? previousCategory = null;
        foreach (var record in records)
        {
            if (previousCategory is not null && previousCategory != record.Category)
            {
                table.AddEmptyRow();
            }

            previousCategory = record.Category;
            table.AddRow(
                ConsoleTheme.CategoryMarkup(record.Category),
                DisplayFormat.DateTime(record.StartedAt),
                $"[bold]{Markup.Escape(record.Car)}[/]",
                ConsoleTheme.PositionMarkup(record),
                record.GridPosition?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? DisplayFormat.Missing,
                DisplayFormat.Lap(record.RaceTime),
                ConsoleTheme.DeltaMarkup(record.RaceDelta),
                record.LapsCompleted?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? DisplayFormat.Missing,
                DisplayFormat.TotalTime(record.FinishTime),
                DisplayFormat.Lap(record.QualifyingTime),
                ConsoleTheme.DeltaMarkup(record.QualifyingDelta));
        }

        return table;
    }
}
