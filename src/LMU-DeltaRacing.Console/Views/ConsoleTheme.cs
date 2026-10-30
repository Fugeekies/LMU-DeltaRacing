using LMU_DeltaRacing.Presentation.Models;
using Spectre.Console;

namespace LMU_DeltaRacing.ConsoleApp.Views;

/// <summary>
/// Colors and shared markup of the console views, taken from the dashboard design specification.
/// </summary>
public static class ConsoleTheme
{
    /// <summary>
    /// Gets the color of a time gain or a positive result.
    /// </summary>
    public static Color Gain { get; } = new(0x22, 0xc5, 0x5e);

    /// <summary>
    /// Gets the color of a time loss, a retirement or an error.
    /// </summary>
    public static Color Loss { get; } = new(0xef, 0x44, 0x44);

    /// <summary>
    /// Gets the color of a class win (WEC purple).
    /// </summary>
    public static Color Win { get; } = new(0xb0, 0x66, 0xff);

    /// <summary>
    /// Gets the color of secondary text and unknown categories.
    /// </summary>
    public static Color Muted { get; } = new(0x8b, 0x94, 0x9e);

    /// <summary>
    /// Gets the colors of the retirement causes, in order of frequency.
    /// </summary>
    public static IReadOnlyList<Color> Causes { get; } =
    [
        new(0xf9, 0x73, 0x16),
        new(0x38, 0xbd, 0xf8),
        new(0xfa, 0xcc, 0x15),
        new(0xf4, 0x72, 0xb6),
        new(0x94, 0xa3, 0xb8)
    ];

    /// <summary>
    /// Returns the official color of a category; unknown classes are grey.
    /// </summary>
    public static Color Category(string category) => category switch
    {
        "Hypercar" => new Color(0xc8, 0x10, 0x2e),
        "LMP2" => new Color(0x14, 0x4a, 0x94),
        "LMP3" => new Color(0x62, 0x30, 0x78),
        "LMGTE" => new Color(0x15, 0x80, 0x3d),
        "LMGT3" => new Color(0xd9, 0x77, 0x06),
        _ => Muted
    };

    /// <summary>
    /// Returns a category name as a badge on its color: the official blue and purple are too dark
    /// to be read as text on a black terminal.
    /// </summary>
    public static string CategoryMarkup(string category)
    {
        ArgumentNullException.ThrowIfNull(category);

        // White text is hard to read on the orange of LMGT3 and the grey of unknown classes.
        var ink = category is "Hypercar" or "LMP2" or "LMP3" or "LMGTE" ? "white" : "black";
        return $"[{ink} on {Category(category).ToMarkup()}] {Markup.Escape(category)} [/]";
    }

    /// <summary>
    /// Returns a lap-time delta colored as a gain (green) or a loss (red).
    /// </summary>
    public static string DeltaMarkup(TimeSpan? delta)
    {
        var text = DisplayFormat.Delta(delta);
        return delta switch
        {
            { } value when value < TimeSpan.Zero => Colored(text, Gain),
            { } value when value > TimeSpan.Zero => Colored(text, Loss),
            _ => Markup.Escape(text)
        };
    }

    /// <summary>
    /// Returns the class position of a "my races" row, highlighted as on the dashboard: a gold,
    /// silver or bronze badge preceded by a trophy for the podium, blue for the top 5 and bold for the top 10.
    /// </summary>
    /// <remarks>
    /// Spectre.Console cannot fill a whole table row, so the highlight is carried by the position cell.
    /// </remarks>
    public static string PositionMarkup(MyRaceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // The trophy of the dashboard; a terminal cannot colour an emoji, the badge gives the colour.
        var label = (record.IsPodium ? "🏆 " : string.Empty) + Markup.Escape(record.ResultLabel);
        return record.PositionTier switch
        {
            MyRacePositionTier.Gold => $"[bold black on #d4af37] {label} [/]",
            MyRacePositionTier.Silver => $"[bold black on #c0c7d1] {label} [/]",
            MyRacePositionTier.Bronze => $"[bold black on #d2733f] {label} [/]",
            MyRacePositionTier.TopFive => $"[bold #60a5fa]{label}[/]",
            MyRacePositionTier.TopTen => $"[bold]{label}[/]",
            _ => label
        };
    }

    /// <summary>
    /// Returns escaped text in the given color.
    /// </summary>
    public static string Colored(string text, Color color) =>
        $"[{color.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Returns escaped text in the secondary color.
    /// </summary>
    public static string MutedMarkup(string text) => Colored(text, Muted);
}
