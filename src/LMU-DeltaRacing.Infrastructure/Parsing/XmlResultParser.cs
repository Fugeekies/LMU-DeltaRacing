using System.Globalization;
using System.Xml;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Domain.Entities;

namespace LMU_DeltaRacing.Infrastructure.Parsing;

/// <summary>
/// Parses LMU result XML files into domain sessions.
/// </summary>
/// <remarks>
/// The document is read as a stream: only the values used by the application are kept, and the
/// event log of the session (<c>Stream</c>: incidents, scores, track limits), which makes most of a
/// race file, is skipped without being loaded. The whole document is still read, so malformed XML
/// is rejected wherever it is. Values are converted once the document has been read.
/// </remarks>
public sealed class XmlResultParser : IResultXmlParser
{
    // Indentation, comments and processing instructions are never read. Values are trimmed, so
    // dropping whitespace-only text changes none of them.
    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreWhitespace = true,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true
    };

    private static readonly string[] ResultsFields = ["DateTime", "TrackVenue", "TrackCourse", "TrackEvent", "TrackLength"];

    private static readonly string[] DriverFields =
    [
        "Name", "CarNumber", "CarClass", "TeamName", "Position", "CarType", "GridPos", "FinishTime",
        "ClassPosition", "Laps", "FinishStatus", "ClassGridPos", "DNFReason"
    ];

    /// <inheritdoc />
    public Session Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The XML stream must be readable.", nameof(stream));
        }

        using var reader = XmlReader.Create(stream, Settings);
        if (reader.MoveToContent() != XmlNodeType.Element)
        {
            throw new XmlException("Root element is missing.");
        }

        RawResults? results = null;
        ReadChildren(reader, () =>
        {
            if (results is null && reader.LocalName == "RaceResults")
            {
                results = ReadResults(reader);
            }
            else
            {
                reader.Skip();
            }
        });

        // Reads to the end so that content after the root element is checked too.
        while (reader.Read())
        {
        }

        return CreateSession(results);
    }

    private static RawResults ReadResults(XmlReader reader)
    {
        var results = new RawResults();
        ReadChildren(reader, () =>
        {
            var field = Array.IndexOf(ResultsFields, reader.LocalName);
            if (field >= 0)
            {
                // Only the first occurrence counts; a repeated element must still be read past.
                ReadFirst(reader, results.Values, field);
            }
            else if (results.Phase is null)
            {
                // The session phase (Practice1, Qualify, Race...) is the first element holding driver results.
                var candidate = ReadPhase(reader);
                if (candidate.Drivers.Count > 0)
                {
                    results.Phase = candidate;
                }
            }
            else
            {
                reader.Skip();
            }
        });

        return results;
    }

    private static RawPhase ReadPhase(XmlReader reader)
    {
        var phase = new RawPhase(reader.LocalName);
        ReadChildren(reader, () =>
        {
            if (reader.LocalName == "MostLapsCompleted")
            {
                if (phase.MostLapsCompleted is null)
                {
                    phase.MostLapsCompleted = ReadText(reader);
                }
                else
                {
                    reader.Skip();
                }

                return;
            }

            var driver = ReadDriver(reader);
            if (driver.HasName && driver.HasPosition)
            {
                phase.Drivers.Add(driver);
            }
        });

        return phase;
    }

    private static RawDriver ReadDriver(XmlReader reader)
    {
        var driver = new RawDriver();
        ReadChildren(reader, () =>
        {
            var name = reader.LocalName;
            var hasNoNamespace = reader.NamespaceURI.Length == 0;
            if (name == "Lap" && hasNoNamespace)
            {
                driver.Laps.Add(new RawLap(
                    reader.GetAttribute("num"),
                    reader.GetAttribute("s1"),
                    reader.GetAttribute("s2"),
                    reader.GetAttribute("s3"),
                    ReadText(reader)));
                return;
            }

            // A driver result is recognized by its Name and Position elements.
            driver.HasName |= name == "Name" && hasNoNamespace;
            driver.HasPosition |= name == "Position" && hasNoNamespace;
            ReadFirst(reader, driver.Values, Array.IndexOf(DriverFields, name));
        });

        return driver;
    }

    /// <summary>
    /// Keeps the text of the current element in <paramref name="values"/> when its field has no value
    /// yet, as the first matching element wins; otherwise skips the element.
    /// </summary>
    private static void ReadFirst(XmlReader reader, string?[] values, int field)
    {
        if (field >= 0 && values[field] is null)
        {
            values[field] = ReadText(reader);
        }
        else
        {
            reader.Skip();
        }
    }

    /// <summary>
    /// Calls <paramref name="readChild"/> on each child element of the current element, which must
    /// read or skip the whole child, then moves the reader past the current element.
    /// </summary>
    private static void ReadChildren(XmlReader reader, Action readChild)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        var depth = reader.Depth;
        reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement || reader.Depth != depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                readChild();
            }
            else
            {
                reader.Read();
            }
        }

        reader.Read();
    }

    /// <summary>
    /// Returns the text of the current element and its descendants, as <c>XElement.Value</c> does,
    /// then moves the reader past the element.
    /// </summary>
    private static string ReadText(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return string.Empty;
        }

        var depth = reader.Depth;
        string? single = null;
        System.Text.StringBuilder? builder = null;
        reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement || reader.Depth != depth)
        {
            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.SignificantWhitespace
                or XmlNodeType.Whitespace)
            {
                if (single is null)
                {
                    single = reader.Value;
                }
                else
                {
                    builder ??= new System.Text.StringBuilder(single);
                    builder.Append(reader.Value);
                }
            }

            reader.Read();
        }

        reader.Read();
        return builder?.ToString() ?? single ?? string.Empty;
    }

    private static Session CreateSession(RawResults? results)
    {
        if (results is null)
        {
            throw new InvalidDataException("The XML document does not contain RaceResults.");
        }

        var phase = results.Phase
            ?? throw new InvalidDataException("The XML document does not contain a driver results session.");
        var timestamp = ParseUnixTimestamp(RequiredValue(results.Values, ResultsFields, "DateTime"));
        var drivers = phase.Drivers.Select(CreateDriver).ToArray();

        return new Session(
            Guid.NewGuid(),
            timestamp,
            RequiredValue(results.Values, ResultsFields, "TrackVenue"),
            RequiredValue(results.Values, ResultsFields, "TrackCourse"),
            Value(results.Values, ResultsFields, "TrackEvent") ?? RequiredValue(results.Values, ResultsFields, "TrackCourse"),
            phase.Name,
            drivers,
            ParseOptionalInt(phase.MostLapsCompleted?.Trim()),
            ParseOptionalMeters(Value(results.Values, ResultsFields, "TrackLength")));
    }

    private static Driver CreateDriver(RawDriver driver)
    {
        var name = RequiredValue(driver.Values, DriverFields, "Name");
        var laps = driver.Laps.Select(CreateLap).ToArray();
        string? Field(string field) => Value(driver.Values, DriverFields, field);

        return new Driver(
            name,
            Field("CarNumber") ?? string.Empty,
            Field("CarClass") ?? string.Empty,
            Field("TeamName") ?? string.Empty,
            ParseOptionalInt(Field("Position")),
            laps,
            Field("CarType") ?? string.Empty,
            ParseOptionalInt(Field("GridPos")),
            ParseOptionalSeconds(Field("FinishTime")),
            ParseOptionalInt(Field("ClassPosition")),
            ParseOptionalInt(Field("Laps")),
            Field("FinishStatus") ?? string.Empty,
            ParseOptionalInt(Field("ClassGridPos")),
            Field("DNFReason") ?? string.Empty);
    }

    private static Lap CreateLap(RawLap lap)
    {
        var sectors = new List<Sector>(3);
        string?[] sectorValues = [lap.Sector1, lap.Sector2, lap.Sector3];
        for (var index = 0; index < sectorValues.Length; index++)
        {
            if (TryParseSeconds(sectorValues[index], out var time))
            {
                sectors.Add(new Sector(index + 1, time));
            }
        }

        return new Lap(
            ParseRequiredInt(lap.Number, "lap number"),
            TryParseSeconds(lap.Time, out var lapTime) ? lapTime : null,
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

    private static double? ParseOptionalMeters(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var meters)
        && double.IsFinite(meters)
        && meters > 0
            ? meters
            : null;

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

    private static string RequiredValue(string?[] values, string[] fields, string name) =>
        Value(values, fields, name) is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"The XML document is missing the required '{name}' value.");

    private static string? Value(string?[] values, string[] fields, string name) =>
        values[Array.IndexOf(fields, name)]?.Trim();

    private sealed class RawResults
    {
        public string?[] Values { get; } = new string?[ResultsFields.Length];

        public RawPhase? Phase { get; set; }
    }

    private sealed class RawPhase(string name)
    {
        public string Name { get; } = name;

        public string? MostLapsCompleted { get; set; }

        public List<RawDriver> Drivers { get; } = [];
    }

    private sealed class RawDriver
    {
        public string?[] Values { get; } = new string?[DriverFields.Length];

        public List<RawLap> Laps { get; } = [];

        public bool HasName { get; set; }

        public bool HasPosition { get; set; }
    }

    private sealed record RawLap(string? Number, string? Sector1, string? Sector2, string? Sector3, string Time);
}
