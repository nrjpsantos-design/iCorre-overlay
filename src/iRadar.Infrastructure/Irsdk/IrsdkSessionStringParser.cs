using System.Globalization;
using System.Text.RegularExpressions;

namespace iRadar.Infrastructure.Irsdk;

// Minimal extractor for the few fields we need from iRacing's YAML session
// string. iRacing emits a regular, predictable shape; full YAML parsing would
// be overkill at Fase 1. If the field set grows, swap this for YamlDotNet.
//
// Strategy: locate top-level blocks by key (WeekendInfo, SessionInfo,
// DriverInfo) and within each block extract fields by simple per-line regex.
// The Drivers list is split on the "- CarIdx:" marker before per-driver parse.
internal static partial class IrsdkSessionStringParser
{
    [GeneratedRegex(@"^\s*TrackName:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex TrackNameRegex();

    [GeneratedRegex(@"^\s*TrackDisplayName:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex TrackDisplayNameRegex();

    [GeneratedRegex(@"^\s*TrackConfigName:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex TrackConfigNameRegex();

    [GeneratedRegex(@"^\s*TrackLength:\s*([\d.]+)\s*km\s*$", RegexOptions.Multiline)]
    private static partial Regex TrackLengthKmRegex();

    [GeneratedRegex(@"^\s*SessionType:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex SessionTypeRegex();

    [GeneratedRegex(@"^\s*DriverCarIdx:\s*(-?\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverCarIdxRegex();

    [GeneratedRegex(@"^\s*CarIdx:\s*(\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverIdxRegex();

    [GeneratedRegex(@"^\s*UserName:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverUserNameRegex();

    [GeneratedRegex(@"^\s*CarNumber:\s*""?([^""\r\n]*?)""?\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverCarNumberRegex();

    [GeneratedRegex(@"^\s*IRating:\s*(-?\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverIRatingRegex();

    [GeneratedRegex(@"^\s*CarClassID:\s*(-?\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex DriverCarClassRegex();

    public static ParsedSessionInfo Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return ParsedSessionInfo.Empty;
        }

        var weekendBlock = ExtractBlock(yaml, "WeekendInfo:", "SessionInfo:") ?? yaml;
        var sessionBlock = ExtractBlock(yaml, "SessionInfo:", "QualifyResultsInfo:") ?? string.Empty;
        var driverBlock = ExtractBlock(yaml, "DriverInfo:", "SplitTimeInfo:") ?? string.Empty;

        var trackName = TrackNameRegex().Match(weekendBlock).Groups[1].Value.Trim();
        var trackDisplay = TrackDisplayNameRegex().Match(weekendBlock).Groups[1].Value.Trim();
        var trackConfig = TrackConfigNameRegex().Match(weekendBlock).Groups[1].Value.Trim();

        var trackLenMeters = 0f;
        var lenMatch = TrackLengthKmRegex().Match(weekendBlock);
        if (lenMatch.Success &&
            float.TryParse(lenMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var km))
        {
            trackLenMeters = km * 1000f;
        }

        var sessionType = SessionTypeRegex().Match(sessionBlock).Groups[1].Value.Trim();

        var driverCarIdx = -1;
        var driverIdxMatch = DriverCarIdxRegex().Match(driverBlock);
        if (driverIdxMatch.Success)
        {
            int.TryParse(driverIdxMatch.Groups[1].Value, out driverCarIdx);
        }

        var drivers = ParseDrivers(driverBlock);

        return new ParsedSessionInfo(
            TrackName: !string.IsNullOrEmpty(trackDisplay) ? trackDisplay : trackName,
            TrackConfigName: trackConfig,
            TrackLengthMeters: trackLenMeters,
            SessionType: sessionType,
            DriverCarIdx: driverCarIdx,
            Drivers: drivers);
    }

    private static IReadOnlyList<ParsedDriver> ParseDrivers(string driverBlock)
    {
        if (string.IsNullOrEmpty(driverBlock)) return Array.Empty<ParsedDriver>();

        var driversIdx = driverBlock.IndexOf("Drivers:", StringComparison.Ordinal);
        if (driversIdx < 0) return Array.Empty<ParsedDriver>();
        var listSlice = driverBlock[driversIdx..];

        var matches = DriverIdxRegex().Matches(listSlice);
        if (matches.Count == 0) return Array.Empty<ParsedDriver>();

        var result = new List<ParsedDriver>(matches.Count);
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : listSlice.Length;
            var entry = listSlice[start..end];

            if (!int.TryParse(matches[i].Groups[1].Value, out var carIdx)) continue;

            var userName = DriverUserNameRegex().Match(entry).Groups[1].Value.Trim();
            var carNumber = DriverCarNumberRegex().Match(entry).Groups[1].Value.Trim();
            int.TryParse(DriverIRatingRegex().Match(entry).Groups[1].Value, out var iRating);
            int.TryParse(DriverCarClassRegex().Match(entry).Groups[1].Value, out var classId);

            result.Add(new ParsedDriver(carIdx, userName, carNumber, iRating, classId));
        }
        return result;
    }

    private static string? ExtractBlock(string yaml, string startKey, string endKey)
    {
        var start = yaml.IndexOf(startKey, StringComparison.Ordinal);
        if (start < 0) return null;
        var end = yaml.IndexOf(endKey, start + startKey.Length, StringComparison.Ordinal);
        return end < 0
            ? yaml[start..]
            : yaml[start..end];
    }
}

internal sealed record ParsedDriver(
    int CarIdx,
    string UserName,
    string CarNumber,
    int IRating,
    int CarClassId);

internal sealed record ParsedSessionInfo(
    string TrackName,
    string TrackConfigName,
    float TrackLengthMeters,
    string SessionType,
    int DriverCarIdx,
    IReadOnlyList<ParsedDriver> Drivers)
{
    public static ParsedSessionInfo Empty { get; } = new(
        TrackName: string.Empty,
        TrackConfigName: string.Empty,
        TrackLengthMeters: 0f,
        SessionType: string.Empty,
        DriverCarIdx: -1,
        Drivers: Array.Empty<ParsedDriver>());
}
