using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Infrastructure.Parsing;

/// <summary>
/// Parses LMU result XML files into domain sessions.
/// </summary>
public sealed class XmlResultParser : IResultXmlParser
{
    /// <inheritdoc />
    public Session Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The XML stream must be readable.", nameof(stream));
        }

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        };

        using var reader = XmlReader.Create(stream, settings);
        var document = XDocument.Load(reader);
        var results = document.Root?.Elements().FirstOrDefault(element => element.Name.LocalName == "RaceResults")
            ?? throw new InvalidDataException("The XML document does not contain RaceResults.");

        var phase = results.Elements()
            .FirstOrDefault(element => element.Elements().Any(IsDriverResult))
            ?? throw new InvalidDataException("The XML document does not contain a driver results session.");

        var timestamp = ParseUnixTimestamp(RequiredValue(results, "DateTime"));
        var drivers = phase.Elements()
            .Where(IsDriverResult)
            .Select(ParseDriver)
            .ToArray();

        return new Session(
            Guid.NewGuid(),
            timestamp,
            RequiredValue(results, "TrackVenue"),
            RequiredValue(results, "TrackCourse"),
            Value(results, "TrackEvent") ?? RequiredValue(results, "TrackCourse"),
            phase.Name.LocalName,
            drivers,
            ParseOptionalInt(Value(phase, "MostLapsCompleted")));
    }

    private static bool IsDriverResult(XElement element) =>
        element.Element("Name") is not null && element.Element("Position") is not null;

    private static Driver ParseDriver(XElement element)
    {
        var name = RequiredValue(element, "Name");
        var laps = element.Elements("Lap").Select(ParseLap).ToArray();

        return new Driver(
            name,
            Value(element, "CarNumber") ?? string.Empty,
            Value(element, "CarClass") ?? string.Empty,
            Value(element, "TeamName") ?? string.Empty,
            ParseOptionalInt(Value(element, "Position")),
            laps,
            Value(element, "CarType") ?? string.Empty,
            ParseOptionalInt(Value(element, "GridPos")),
            ParseOptionalSeconds(Value(element, "FinishTime")),
            ParseOptionalInt(Value(element, "ClassPosition")),
            ParseOptionalInt(Value(element, "Laps")),
            Value(element, "FinishStatus") ?? string.Empty);
    }

    private static Lap ParseLap(XElement element)
    {
        var sectors = new List<Sector>(3);
        for (var number = 1; number <= 3; number++)
        {
            var value = element.Attribute($"s{number}")?.Value;
            if (TryParseSeconds(value, out var time))
            {
                sectors.Add(new Sector(number, time));
            }
        }

        return new Lap(
            ParseRequiredInt(element.Attribute("num")?.Value, "lap number"),
            TryParseSeconds(element.Value, out var lapTime) ? lapTime : null,
            sectors);
    }

    private static DateTimeOffset ParseUnixTimestamp(string value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            throw new InvalidDataException($"The session timestamp '{value}' is not a valid Unix timestamp.");
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException($"The session timestamp '{value}' is out of range.", exception);
        }
    }

    private static int? ParseOptionalInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static TimeSpan? ParseOptionalSeconds(string? value) =>
        TryParseSeconds(value, out var time) ? time : null;

    private static int ParseRequiredInt(string? value, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0)
        {
            throw new InvalidDataException($"The {fieldName} '{value}' is not a valid positive integer.");
        }

        return result;
    }

    private static bool TryParseSeconds(string? value, out TimeSpan time)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && double.IsFinite(seconds)
            && seconds >= 0)
        {
            time = TimeSpan.FromSeconds(seconds);
            return true;
        }

        time = default;
        return false;
    }

    private static string RequiredValue(XElement element, string name) =>
        Value(element, name) is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"The XML document is missing the required '{name}' value.");

    private static string? Value(XElement element, string name) =>
        element.Elements().FirstOrDefault(child => child.Name.LocalName == name)?.Value.Trim();
}
