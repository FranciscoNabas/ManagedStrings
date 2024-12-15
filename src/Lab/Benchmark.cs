// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ManagedStrings.Engine.Console;

namespace ManagedStrings.Lab;

/// <summary>
/// Extension methods for numbers.
/// </summary>
internal static partial class NumericExtensions
{
    internal static int GetUInt32DigitCount(this uint n)
        => n == 0 ? 1 : (n > 0 ? 1 : 2) + (int)Math.Log10(Math.Abs((double)n));
}

/// <summary>
/// The base class for all benchmark information.
/// </summary>
internal abstract class BenchmarkData
{
    protected static readonly FileTimeNameComparer FileNameComparer = new();

    protected const int TimeColumnLength = 11;
    protected const int TimeColumnRemainingLength = 5;

    protected static string ItemsString  = "Items";
    protected static string TimeString   = "Time";
    protected static string TotalString  = "Total";

    internal TimeSpan TotalTime { get; set; }
    internal List<SingleBenchmarkTime> SingleTimeList { get; } = [];

    /// <summary>
    /// Prints benchmark information to the console.
    /// </summary>
    internal abstract void PrintData();
    
    /// <summary>
    /// Prints the benchmark header.
    /// </summary>
    /// <param name="process">True if it's <see cref="ProcessBenchmarkData"/>.</param>
    /// <returns>The length of the biggest text in the first column.</returns>
    protected int PrintHeader(bool process)
    {
        WindowsConsole.WriteLine();
        WindowsConsole.WriteLine();

        int column1Length;
        int biggestFileName = Path.GetFileName(SingleTimeList.OrderByDescending(d => d, FileNameComparer).First().Path).Length;
        if (process) {
            IEnumerable<SingleProcessTime> processTimes = SingleTimeList.Cast<SingleProcessTime>();
            int biggestId = processTimes.OrderByDescending(t => t.ProcessId.GetUInt32DigitCount()).First().ProcessId.GetUInt32DigitCount();
            column1Length = biggestId + biggestFileName + 7;
        }
        else
            column1Length = biggestFileName + 2;

        StringBuilder sb = new();
        sb.AppendLine("---- BENCHMARK RESULT ----\n");
        sb.Append('+');
        for (int i = 0; i < column1Length; ++i)
            sb.Append('-');

        sb.Append('+');
        for (int i = 0; i < TimeColumnLength; ++i)
            sb.Append('-');

        sb.AppendLine("+");

        sb.Append($"| {ItemsString}");
        for (int i = 0; i < Math.Abs(ItemsString.Length - column1Length) - 1; i++)
            sb.Append(' ');

        sb.Append('|');
        sb.Append($" {TimeString}");
        for (int i = 0; i < TimeColumnRemainingLength; i++)
            sb.Append(' ');

        sb.AppendLine(" |");
        sb.Append('+');
        for (int i = 0; i < column1Length; ++i)
            sb.Append('-');

        sb.Append('+');
        for (int i = 0; i < TimeColumnLength; ++i)
            sb.Append('-');

        sb.Append('+');

        WindowsConsole.WriteLine(sb.ToString());

        return column1Length;
    }

    /// <summary>
    /// Prints the total time column.
    /// </summary>
    /// <param name="column1Length">The maximum length for the first column.</param>
    protected void PrintTotalTime(int column1Length)
    {
        StringBuilder sb = new();
        sb.Append($"| {TotalString}");
        for (int i = 0; i < Math.Abs(TotalString.Length - column1Length) - 1; i++)
            sb.Append(' ');

        string totalTimeString = TotalTime.TotalMilliseconds switch {
            double mills when mills < 10 => Math.Round(TotalTime.TotalMilliseconds, 4).ToString("0.0000") + " ms",
            double mills when mills >= 10 && mills < 100 => Math.Round(TotalTime.TotalMilliseconds, 3).ToString("0.000") + " ms",
            double mills when mills >= 100 && mills < 1000 => Math.Round(TotalTime.TotalMilliseconds, 2).ToString("0.00") + " ms",
            double mills when mills >= 1000 && mills < 10000 => Math.Round(TotalTime.TotalSeconds, 4).ToString("0.0000") + " s ",
            double mills when mills >= 10000 && mills < 60000 => Math.Round(TotalTime.TotalSeconds, 3).ToString("0.000") + " s ",
            double mills when mills >= 60000 && mills < 600000 => Math.Round(TotalTime.TotalMinutes, 4).ToString("0.0000") + " m ",
            double mills when mills >= 600000 && mills < 3600000 => Math.Round(TotalTime.TotalMinutes, 3).ToString("0.000") + " m ",
            double mills when mills >= 3600000 && mills < 36000000 => Math.Round(TotalTime.TotalHours, 4).ToString("0.0000") + " h ",
            double mills when mills >= 36000000 && mills < 86400000 => Math.Round(TotalTime.TotalHours, 3).ToString("0.000") + " h ",
            _ => Math.Round(TotalTime.TotalDays, 4).ToString("0.0000") + " d ",
        };

        sb.Append($"| {totalTimeString} |");
        WindowsConsole.WriteLine(sb.ToString());
    }
}

/// <summary>
/// Contains file processing benchmark information.
/// </summary>
internal sealed class FileBenchmarkData : BenchmarkData
{
    /// <summary>
    /// Prints benchmark information to the console.
    /// </summary>
    internal override void PrintData()
    {
        WindowsConsole.Flush();
        ConsoleColor originalColor = WindowsConsole.ForegroundColor;
        WindowsConsole.ForegroundColor = ConsoleColor.Cyan;
        
        int column1Length = PrintHeader(false);
        PrintTotalTime(column1Length);
        
        StringBuilder sb = new();
        sb.Clear();
        foreach (SingleBenchmarkTime item in SingleTimeList.OrderBy(t => t.Time.TotalMilliseconds)) {
            string currentFileName = Path.GetFileName(item.Path);
            int column1Remaining = column1Length - currentFileName.Length;
            sb.Append($"| {currentFileName}");
            for (int i = 0; i < column1Remaining - 1; i++)
                sb.Append(' ');

            string fileTimeString = item.Time.TotalMilliseconds switch {
                double mills when mills < 10 => Math.Round(item.Time.TotalMilliseconds, 4).ToString("0.0000") + " ms",
                double mills when mills >= 10 && mills < 100 => Math.Round(item.Time.TotalMilliseconds, 3).ToString("0.000") + " ms",
                double mills when mills >= 100 && mills < 1000 => Math.Round(item.Time.TotalMilliseconds, 2).ToString("0.00") + " ms",
                double mills when mills >= 1000 && mills < 10000 => Math.Round(item.Time.TotalSeconds, 4).ToString("0.0000") + " s ",
                double mills when mills >= 10000 && mills < 60000 => Math.Round(item.Time.TotalSeconds, 3).ToString("0.000") + " s ",
                double mills when mills >= 60000 && mills < 600000 => Math.Round(item.Time.TotalMinutes, 4).ToString("0.0000") + " m ",
                double mills when mills >= 600000 && mills < 3600000 => Math.Round(item.Time.TotalMinutes, 3).ToString("0.000") + " m ",
                double mills when mills >= 3600000 && mills < 36000000 => Math.Round(item.Time.TotalHours, 4).ToString("0.0000") + " h ",
                double mills when mills >= 36000000 && mills < 86400000 => Math.Round(item.Time.TotalHours, 3).ToString("0.000") + " h ",
                _ => Math.Round(item.Time.TotalDays, 4).ToString("0.0000") + " d ",
            };

            sb.Append($"| {fileTimeString} |");
            WindowsConsole.WriteLine(sb.ToString());

            sb.Clear();
        }

        sb.Append('+');
        for (int i = 0; i < column1Length; ++i)
            sb.Append('-');

        sb.Append('+');
        for (int i = 0; i < TimeColumnLength; ++i)
            sb.Append('-');

        sb.Append('+');
        WindowsConsole.WriteLine(sb.ToString());
        WindowsConsole.WriteLine();

        WindowsConsole.Flush();
        WindowsConsole.ForegroundColor = originalColor;
    }
}

/// <summary>
/// Contains process processing benchmark information.
/// </summary>
internal sealed class ProcessBenchmarkData : BenchmarkData
{
    /// <summary>
    /// Prints benchmark information to the console.
    /// </summary>
    internal override void PrintData()
    {
        WindowsConsole.Flush();
        ConsoleColor originalColor = WindowsConsole.ForegroundColor;
        WindowsConsole.ForegroundColor = ConsoleColor.Cyan;

        int column1Length = PrintHeader(true);
        PrintTotalTime(column1Length);

        StringBuilder sb = new();
        sb.Clear();
        foreach (SingleProcessTime item in SingleTimeList.OrderBy(t => t.Time.TotalMilliseconds).Cast<SingleProcessTime>()) {
            string currentProcessText = $"[{item.ProcessId}] - {Path.GetFileName(item.Path)}";
            int column1Remaining = column1Length - currentProcessText.Length;
            sb.Append($"| {currentProcessText}");
            for (int i = 0; i < column1Remaining - 1; i++)
                sb.Append(' ');

            string processTimeString = item.Time.TotalMilliseconds switch {
                double mills when mills < 10 => Math.Round(item.Time.TotalMilliseconds, 4).ToString("0.0000") + " ms",
                double mills when mills >= 10 && mills < 100 => Math.Round(item.Time.TotalMilliseconds, 3).ToString("0.000") + " ms",
                double mills when mills >= 100 && mills < 1000 => Math.Round(item.Time.TotalMilliseconds, 2).ToString("0.00") + " ms",
                double mills when mills >= 1000 && mills < 10000 => Math.Round(item.Time.TotalSeconds, 4).ToString("0.0000") + " s ",
                double mills when mills >= 10000 && mills < 60000 => Math.Round(item.Time.TotalSeconds, 3).ToString("0.000") + " s ",
                double mills when mills >= 60000 && mills < 600000 => Math.Round(item.Time.TotalMinutes, 4).ToString("0.0000") + " m ",
                double mills when mills >= 600000 && mills < 3600000 => Math.Round(item.Time.TotalMinutes, 3).ToString("0.000") + " m ",
                double mills when mills >= 3600000 && mills < 36000000 => Math.Round(item.Time.TotalHours, 4).ToString("0.0000") + " h ",
                double mills when mills >= 36000000 && mills < 86400000 => Math.Round(item.Time.TotalHours, 3).ToString("0.000") + " h ",
                _ => Math.Round(item.Time.TotalDays, 4).ToString("0.0000") + " d ",
            };

            sb.Append($"| {processTimeString} |");
            WindowsConsole.WriteLine(sb.ToString());

            sb.Clear();
        }

        sb.Append('+');
        for (int i = 0; i < column1Length; ++i)
            sb.Append('-');

        sb.Append('+');
        for (int i = 0; i < TimeColumnLength; ++i)
            sb.Append('-');

        sb.Append('+');
        WindowsConsole.WriteLine(sb.ToString());
        WindowsConsole.WriteLine();

        WindowsConsole.Flush();
        WindowsConsole.ForegroundColor = originalColor;
    }
}

/// <summary>
/// The base class for all benchmark single times.
/// </summary>
/// <param name="path">The benchmark object path.</param>
/// <param name="time">The processing time span.</param>
internal abstract class SingleBenchmarkTime(string path, TimeSpan time)
{
    internal string Path { get; } = path;
    internal TimeSpan Time { get; } = time;
}

/// <summary>
/// A single file processing time.
/// </summary>
/// <param name="path">The file path.</param>
/// <param name="time">The processing time span.</param>
internal sealed class SingleFileTime(string path, TimeSpan time) : SingleBenchmarkTime(path, time)
{
    public override string ToString() => Path;
}

/// <summary>
/// A single process processing time.
/// </summary>
/// <param name="processId">The process ID.</param>
/// <param name="imagePath">The process image path.</param>
/// <param name="time">The processing time span.</param>
internal sealed class SingleProcessTime(uint processId, string imagePath, TimeSpan time) : SingleBenchmarkTime(imagePath, time)
{
    internal uint ProcessId { get; } = processId;

    public override string ToString() => $"[{ProcessId}] {Path}";
}

/// <summary>
/// A comparer for file names.
/// </summary>
internal sealed class FileTimeNameComparer : Comparer<SingleBenchmarkTime>
{
    /// <summary>
    /// Compares the file name from the path from a <see cref="SingleBenchmarkTime"/>.
    /// </summary>
    /// <param name="x">The left <see cref="SingleBenchmarkTime"/>.</param>
    /// <param name="y">The right <see cref="SingleBenchmarkTime"/>.</param>
    /// <returns>
    /// 0 if the file names are equal.
    /// Negative number if x is less than y.
    /// Positive number if x is more than y.   
    /// </returns>
    /// <remarks>
    /// null is considered to be less than any instance, hence returns positive number.
    /// </remarks>
    public override int Compare(SingleBenchmarkTime? x, SingleBenchmarkTime? y)
    {
        if (x is null && y is null)
            return 0;

        if (x is null && y is not null)
            return -1;

        if (x is not null && y is null)
            return 1;

        return Path.GetFileName(x!.Path).Length.CompareTo(Path.GetFileName(y!.Path).Length);
    }
}