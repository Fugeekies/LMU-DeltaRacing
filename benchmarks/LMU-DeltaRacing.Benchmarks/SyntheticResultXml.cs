using System.Globalization;
using System.Text;
using System.Xml;

namespace LMU_DeltaRacing.Benchmarks;

internal static class SyntheticResultXml
{
    private static readonly (string SessionType, int LapCount)[] Sessions =
    [
        ("Practice1", 3),
        ("Qualifying", 2),
        ("Race", 8)
    ];

    public static IReadOnlyList<LoadBenchmarkRunner.PhaseStream> CreatePhaseStreams() =>
        Sessions.Select(session =>
        {
            var bytes = CreateDocument(session.SessionType, session.LapCount);
            return new LoadBenchmarkRunner.PhaseStream(
                bytes,
                new MemoryStream(bytes, writable: false));
        }).ToArray();

    private static byte[] CreateDocument(string sessionType, int lapCount)
    {
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
               {
                   Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                   Indent = false,
                   CloseOutput = false
               }))
        {
            writer.WriteStartElement("rFactorXML");
            writer.WriteStartElement("RaceResults");
            WriteElement(writer, "DateTime", "1760000000");
            WriteElement(writer, "TrackVenue", "Sebring International Raceway");
            WriteElement(writer, "TrackCourse", "Sebring School Circuit");
            WriteElement(writer, "TrackEvent", "Benchmark Event");
            writer.WriteStartElement(sessionType);

            for (var driverIndex = 1; driverIndex <= 10; driverIndex++)
            {
                writer.WriteStartElement("Driver");
                WriteElement(writer, "Name", $"Benchmark Driver {driverIndex:D2}");
                WriteElement(writer, "Position", driverIndex.ToString(CultureInfo.InvariantCulture));
                WriteElement(writer, "CarNumber", driverIndex.ToString(CultureInfo.InvariantCulture));
                WriteElement(writer, "CarClass", "Hypercar");
                WriteElement(writer, "TeamName", $"Benchmark Team {driverIndex:D2}");

                for (var lapNumber = 1; lapNumber <= lapCount; lapNumber++)
                {
                    writer.WriteStartElement("Lap");
                    writer.WriteAttributeString("num", lapNumber.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("s1", "22.8031");
                    writer.WriteAttributeString("s2", "12.8257");
                    writer.WriteAttributeString("s3", "29.5404");
                    writer.WriteString("65.1692");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Flush();
        }

        return stream.ToArray();
    }

    private static void WriteElement(XmlWriter writer, string name, string value)
    {
        writer.WriteElementString(name, value);
    }
}
