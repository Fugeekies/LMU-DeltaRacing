param(
    # The original (not anonymized) result files. They are never copied: only the link between their
    # names and the drivers of each fixture is used, so no original name can reach the fixtures.
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    # The anonymized fixtures, repaired in place.
    [Parameter(Mandatory = $true)]
    [string]$FixtureDirectory,

    # Reports what would change without writing anything.
    [switch]$CheckOnly
)

# Makes the names of the Stream events match the drivers listed in anonymized fixtures, a link lost
# by an earlier anonymization, without regenerating them: aliases, hand-made demonstration edits
# and everything outside <Stream> stay byte for byte.
# - Drivers are paired by their position in the file, after checking that they hold the same laps.
#   Names therefore follow the laps: in a session where two identities were swapped by hand, the
#   events of a car go to the driver now shown with that car.
# - Stream aliases are paired with original names event by event.
# - A name that belongs to no listed driver keeps its alias: it matches nobody, so its events are
#   ignored rather than attributed by chance.
# Written in C# because the fixtures hold about 100 000 events.

$ErrorActionPreference = 'Stop'

if (-not ('LmuStreamNameRepair' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public sealed class StreamRepairResult
{
    public int FilesChanged;
    public int NamesLinked;
    public int NamesKept;
    public int NamesSettledByEvent;
    public int SharedOriginalNames;
    public int EventsChanged;
    public List<string> Problems = new List<string>();
}

public static class LmuStreamNameRepair
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
    private static readonly Regex DriverBlock = new Regex(@"<Driver>.*?</Driver>", Options | RegexOptions.Singleline);
    private static readonly Regex Name = new Regex(@"<Name>([^<]*)</Name>", Options);
    private static readonly Regex Swap = new Regex(@"<Swap\b[^>]*>([^<]*)</Swap>", Options);
    private static readonly Regex Lap = new Regex(@"<Lap\b[^>]*>[^<]*</Lap>", Options);
    private static readonly Regex LapValue = new Regex(@"\bnum=""(\d+)"".*?>([^<]*)</Lap>", Options);
    private static readonly Regex StreamBlock = new Regex(@"<Stream>.*?</Stream>", Options | RegexOptions.Singleline);
    private static readonly Regex Event = new Regex(@"<([A-Za-z]+)\b([^>]*)>([^<]*)</\1>", Options);
    private static readonly Regex DriverAttribute = new Regex(@"\bDriver=""([^""]*)""", Options);
    private static readonly Regex Leading = new Regex(@"^(.+?)\(\d+\) (?:reported contact|lap=|reports )", Options);
    private static readonly Regex Other = new Regex(@"another vehicle (.+?)\(\d+\)\s*$", Options);
    private static readonly Regex Wheel = new Regex(@"^&quot;(.+)&quot; has taken the wheel", Options);

    public static StreamRepairResult Repair(string sourceDirectory, string fixtureDirectory, bool checkOnly)
    {
        var result = new StreamRepairResult();
        foreach (var fixturePath in Directory.GetFiles(fixtureDirectory, "*.xml").OrderBy(path => path, StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(fixturePath);
            var sourcePath = Path.Combine(sourceDirectory, fileName);
            if (!File.Exists(sourcePath))
            {
                result.Problems.Add(fileName + ": no original file");
                continue;
            }

            var encoding = new UTF8Encoding(false);
            var fixtureBytes = File.ReadAllBytes(fixturePath);
            var bom = fixtureBytes.Length >= 3 && fixtureBytes[0] == 0xEF && fixtureBytes[1] == 0xBB && fixtureBytes[2] == 0xBF;
            var fixture = encoding.GetString(fixtureBytes, bom ? 3 : 0, fixtureBytes.Length - (bom ? 3 : 0));
            var repaired = RepairFile(fileName, File.ReadAllText(sourcePath), fixture, result);
            if (repaired == null || repaired == fixture)
            {
                continue;
            }

            result.FilesChanged++;
            if (!checkOnly)
            {
                var body = encoding.GetBytes(repaired);
                File.WriteAllBytes(fixturePath, bom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray() : body);
            }
        }

        return result;
    }

    private static string RepairFile(string fileName, string source, string fixture, StreamRepairResult result)
    {
        var sourceDrivers = DriverBlock.Matches(source).Cast<Match>().Select(match => match.Value).ToArray();
        var fixtureDrivers = DriverBlock.Matches(fixture).Cast<Match>().Select(match => match.Value).ToArray();
        if (sourceDrivers.Length != fixtureDrivers.Length)
        {
            result.Problems.Add(fileName + ": " + sourceDrivers.Length + " original drivers, " + fixtureDrivers.Length + " in the fixture");
            return null;
        }

        var listed = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < sourceDrivers.Length; index++)
        {
            if (!SameLaps(sourceDrivers[index], fixtureDrivers[index]))
            {
                result.Problems.Add(fileName + ": laps differ for driver " + index);
                return null;
            }

            AddListed(listed, Name.Match(sourceDrivers[index]).Groups[1].Value, Name.Match(fixtureDrivers[index]).Groups[1].Value, fileName, result);
            var sourceSwaps = Swap.Matches(sourceDrivers[index]);
            var fixtureSwaps = Swap.Matches(fixtureDrivers[index]);
            for (var swap = 0; swap < Math.Min(sourceSwaps.Count, fixtureSwaps.Count); swap++)
            {
                AddListed(listed, sourceSwaps[swap].Groups[1].Value, fixtureSwaps[swap].Groups[1].Value, fileName, result);
            }
        }

        var sourceStream = StreamBlock.Match(source);
        var fixtureStream = StreamBlock.Match(fixture);
        if (!sourceStream.Success || !fixtureStream.Success)
        {
            result.Problems.Add(fileName + ": no Stream");
            return null;
        }

        var sourceEvents = Event.Matches(sourceStream.Value);
        var fixtureEvents = Event.Matches(fixtureStream.Value);
        if (sourceEvents.Count != fixtureEvents.Count)
        {
            result.Problems.Add(fileName + ": " + sourceEvents.Count + " original events, " + fixtureEvents.Count + " in the fixture");
            return null;
        }

        var aliases = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        for (var index = 0; index < sourceEvents.Count; index++)
        {
            if (sourceEvents[index].Groups[1].Value != fixtureEvents[index].Groups[1].Value)
            {
                result.Problems.Add(fileName + ": event " + index + " differs");
                return null;
            }

            foreach (var pair in NamePairs(sourceEvents[index], fixtureEvents[index]))
            {
                HashSet<string> names;
                if (!aliases.TryGetValue(pair.Value, out names))
                {
                    aliases[pair.Value] = names = new HashSet<string>(StringComparer.Ordinal);
                }

                names.Add(pair.Key);
            }
        }

        // An alias standing for one name applies everywhere, including events that give no pair
        // (chat senders...). One standing for several names is settled event by event.
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var alias in aliases)
        {
            string target;
            if (alias.Value.Count > 1)
            {
                result.NamesSettledByEvent++;
            }
            else if (listed.TryGetValue(Normalize(alias.Value.Single()), out target) && target != null)
            {
                replacements[alias.Key] = target;
                result.NamesLinked++;
            }
            else
            {
                result.NamesKept++;
            }
        }

        if (aliases.Count == 0)
        {
            return fixture;
        }

        var pattern = new Regex(
            "(?<![\\p{L}\\d])(?:" + string.Join("|", aliases.Keys.OrderByDescending(key => key.Length).Select(Regex.Escape)) + ")(?![\\p{L}\\d])",
            Options);
        var position = 0;
        var stream = Event.Replace(fixtureStream.Value, match =>
        {
            var local = new Dictionary<string, string>(replacements, StringComparer.Ordinal);
            foreach (var pair in NamePairs(sourceEvents[position], fixtureEvents[position]))
            {
                string target;
                if (aliases[pair.Value].Count > 1 && listed.TryGetValue(Normalize(pair.Key), out target) && target != null)
                {
                    local[pair.Value] = target;
                }
            }

            position++;
            var replaced = pattern.Replace(match.Value, name =>
            {
                string target;
                return local.TryGetValue(name.Value, out target) ? target : name.Value;
            });
            if (replaced != match.Value)
            {
                result.EventsChanged++;
            }

            return replaced;
        });
        return fixture.Substring(0, fixtureStream.Index) + stream + fixture.Substring(fixtureStream.Index + fixtureStream.Length);
    }

    private static void AddListed(Dictionary<string, string> listed, string source, string alias, string fileName, StreamRepairResult result)
    {
        var key = Normalize(source);
        string existing;
        if (listed.TryGetValue(key, out existing) && existing != alias)
        {
            // Nothing tells which of the two an event is about: both are left out, not guessed.
            listed[key] = null;
            result.SharedOriginalNames++;
            return;
        }

        listed[key] = alias;
    }

    private static IEnumerable<KeyValuePair<string, string>> NamePairs(Match source, Match fixture)
    {
        var sourceAttribute = DriverAttribute.Match(source.Groups[2].Value);
        var fixtureAttribute = DriverAttribute.Match(fixture.Groups[2].Value);
        if (sourceAttribute.Success && fixtureAttribute.Success)
        {
            yield return new KeyValuePair<string, string>(sourceAttribute.Groups[1].Value, fixtureAttribute.Groups[1].Value);
        }

        foreach (var regex in new[] { Leading, Other, Wheel })
        {
            var sourceName = regex.Match(source.Groups[3].Value);
            var fixtureName = regex.Match(fixture.Groups[3].Value);
            if (sourceName.Success && fixtureName.Success)
            {
                yield return new KeyValuePair<string, string>(sourceName.Groups[1].Value, fixtureName.Groups[1].Value);
            }
        }
    }

    private static bool SameLaps(string source, string fixture)
    {
        var sourceLaps = Lap.Matches(source).Cast<Match>().Select(LapKey).ToArray();
        var fixtureLaps = Lap.Matches(fixture).Cast<Match>().Select(LapKey).ToArray();
        return sourceLaps.SequenceEqual(fixtureLaps);
    }

    private static string LapKey(Match lap)
    {
        var value = LapValue.Match(lap.Value);
        return value.Groups[1].Value + ":" + value.Groups[2].Value;
    }

    private static string Normalize(string name) => WebUtility.HtmlDecode(name).Trim();
}
'@
}

$result = [LmuStreamNameRepair]::Repair(
    [System.IO.Path]::GetFullPath($SourceDirectory),
    [System.IO.Path]::GetFullPath($FixtureDirectory),
    $CheckOnly.IsPresent)

Write-Output "Files changed: $($result.FilesChanged). Names linked: $($result.NamesLinked), kept: $($result.NamesKept), settled by event: $($result.NamesSettledByEvent). Original names shared by two drivers: $($result.SharedOriginalNames). Events changed: $($result.EventsChanged). Problems: $($result.Problems.Count)."
$result.Problems | Select-Object -First 20
if ($result.Problems.Count -gt 0) { exit 1 }
