#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Text;

namespace NINA.Benchmarks;

public static class Program {
    private const string ArtifactsDirectoryName = "BenchmarkDotNet.Artifacts";
    private const string ResultsDirectoryName = "Results";

    public static int Main(string[] args) {
        string projectRootPath = GetProjectRootPath();
        string artifactsPath = Path.Combine(projectRootPath, ArtifactsDirectoryName);
        string resultsPath = Path.Combine(projectRootPath, ResultsDirectoryName);

        var config = ManualConfig.Create(DefaultConfig.Instance);
        config.ArtifactsPath = artifactsPath;

        Summary[] summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config)
            .ToArray();

        string comparisonPath = BenchmarkResultsWriter.WriteComparisonSummary(summaries, resultsPath, args);
        Console.WriteLine();
        Console.WriteLine($"Comparison benchmark summary: {comparisonPath}");
        return 0;
    }

    private static string GetProjectRootPath() {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}

internal static class BenchmarkResultsWriter {
    private const string ComparisonResultsFileName = "benchmark-results.txt";
    private static readonly string[] Headers = ["Method", "Mean", "Error", "StdDev", "Median", "Ratio", "RatioSD", "Allocated", "Alloc Ratio"];

    public static string WriteComparisonSummary(IEnumerable<Summary> summaries, string resultsDirectoryPath, IEnumerable<string> args) {
        Directory.CreateDirectory(resultsDirectoryPath);
        string resultsPath = Path.Combine(resultsDirectoryPath, ComparisonResultsFileName);
        string commandLine = string.Join(" ", args);

        using var writer = new StreamWriter(resultsPath, false, new UTF8Encoding(false));

        writer.WriteLine("NINA Benchmark Comparisons");
        writer.WriteLine($"Generated: {DateTimeOffset.Now:O}");
        writer.WriteLine($"Command: {(string.IsNullOrWhiteSpace(commandLine) ? "<none>" : commandLine)}");
        writer.WriteLine();

        bool wroteAnySummary = false;
        foreach (Summary summary in summaries) {
            var groups = CreateGroups(summary).ToList();
            if (groups.Count == 0) {
                continue;
            }

            wroteAnySummary = true;
            string title = summary.BenchmarksCases.Length > 0 ? summary.BenchmarksCases[0].Descriptor.Type.Name : summary.Title;
            writer.WriteLine(title);
            writer.WriteLine(new string('=', title.Length));
            writer.WriteLine();

            foreach (ComparisonGroup group in groups) {
                writer.WriteLine(group.Label);
                writer.WriteLine();
                WriteTable(writer, group.Rows);
                writer.WriteLine();
            }
        }

        if (!wroteAnySummary) {
            writer.WriteLine("No benchmarks were executed.");
        }

        return resultsPath;
    }

    private static IEnumerable<ComparisonGroup> CreateGroups(Summary summary) {
        return summary.Reports
            .Where(report => report.Success && report.ResultStatistics != null)
            .GroupBy(report => GetGroupKey(report.BenchmarkCase))
            .Select(group => CreateGroup(group.Key, group.ToList()))
            .OrderBy(group => group.Label, StringComparer.Ordinal);
    }

    private static ComparisonGroup CreateGroup(string key, List<BenchmarkReport> reports) {
        reports = reports
            .OrderByDescending(report => report.BenchmarkCase.Descriptor.Baseline)
            .ThenBy(report => GetDisplayMethodName(report.BenchmarkCase), StringComparer.Ordinal)
            .ToList();

        BenchmarkReport baseline = reports.FirstOrDefault(report => report.BenchmarkCase.Descriptor.Baseline) ?? reports[0];
        double baselineMean = baseline.ResultStatistics!.Mean;
        double baselineStdDev = baseline.ResultStatistics.StandardDeviation;
        long? baselineAllocated = baseline.GcStats.GetBytesAllocatedPerOperation(baseline.BenchmarkCase);

        var rows = reports.Select(report => CreateRow(report, baseline, baselineMean, baselineStdDev, baselineAllocated)).ToList();
        return new ComparisonGroup(key, rows);
    }

    private static string[] CreateRow(BenchmarkReport report, BenchmarkReport baseline, double baselineMean, double baselineStdDev, long? baselineAllocated) {
        var stats = report.ResultStatistics!;
        bool isBaseline = ReferenceEquals(report, baseline);
        long? allocated = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);

        double ratio = baselineMean == 0 ? 0 : stats.Mean / baselineMean;
        double? ratioSd = isBaseline ? null : CalculateRatioStdDev(stats.Mean, stats.StandardDeviation, baselineMean, baselineStdDev, ratio);
        double? allocRatio = (!baselineAllocated.HasValue || baselineAllocated.Value == 0 || !allocated.HasValue)
            ? null
            : allocated.Value / (double)baselineAllocated.Value;

        return [
            GetDisplayMethodName(report.BenchmarkCase),
            FormatDuration(stats.Mean),
            FormatDuration(stats.ConfidenceInterval.Margin),
            FormatDuration(stats.StandardDeviation),
            FormatDuration(stats.Median),
            FormatRatio(ratio),
            isBaseline ? "-" : FormatRatio(ratioSd),
            FormatBytes(allocated),
            isBaseline ? "1.00" : FormatRatio(allocRatio)
        ];
    }

    private static double? CalculateRatioStdDev(double mean, double stdDev, double baselineMean, double baselineStdDev, double ratio) {
        if (mean == 0 || baselineMean == 0) {
            return null;
        }

        double meanCv = stdDev / mean;
        double baselineCv = baselineStdDev / baselineMean;
        return Math.Abs(ratio) * Math.Sqrt((meanCv * meanCv) + (baselineCv * baselineCv));
    }

    private static string GetGroupKey(BenchmarkCase benchmarkCase) {
        var groupItems = benchmarkCase.Parameters.Items.Where(item => !IsThreadParameter(item.Name)).ToList();
        if (groupItems.Count == 0) {
            return "Parameters: <none>";
        }

        return string.Join(", ", groupItems.Select(item => $"{item.Name}: {item.Value}"));
    }

    private static string GetDisplayMethodName(BenchmarkCase benchmarkCase) {
        string methodName = benchmarkCase.Descriptor.WorkloadMethod.Name;
        var threadItem = benchmarkCase.Parameters.Items.FirstOrDefault(item => IsThreadParameter(item.Name));
        if (threadItem == null) {
            return methodName;
        }

        return $"{methodName} (Threads={threadItem.Value})";
    }

    private static bool IsThreadParameter(string name) {
        return name.Equals("threads", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDuration(double nanoseconds) {
        if (double.IsNaN(nanoseconds) || double.IsInfinity(nanoseconds)) {
            return "-";
        }

        if (Math.Abs(nanoseconds) >= 1_000_000) {
            return $"{nanoseconds / 1_000_000:0.00} ms";
        }

        if (Math.Abs(nanoseconds) >= 1_000) {
            return $"{nanoseconds / 1_000:0.00} us";
        }

        return $"{nanoseconds:0.00} ns";
    }

    private static string FormatBytes(long? bytes) {
        if (!bytes.HasValue) {
            return "-";
        }

        double value = bytes.Value;
        if (Math.Abs(value) >= 1024 * 1024) {
            return $"{value / (1024 * 1024):0.00} MB";
        }

        if (Math.Abs(value) >= 1024) {
            return $"{value / 1024:0.00} KB";
        }

        return $"{value:0} B";
    }

    private static string FormatRatio(double? value) {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) {
            return "-";
        }

        return value.Value.ToString("0.00");
    }

    private static void WriteTable(StreamWriter writer, IReadOnlyList<string[]> rows) {
        int[] widths = CalculateColumnWidths(rows);
        WriteRow(writer, Headers, widths);
        WriteSeparator(writer, widths);

        foreach (string[] row in rows) {
            WriteRow(writer, row, widths);
        }
    }

    private static int[] CalculateColumnWidths(IReadOnlyList<string[]> rows) {
        int[] widths = Headers.Select(header => header.Length).ToArray();

        foreach (string[] row in rows) {
            for (int i = 0; i < row.Length; i++) {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        return widths;
    }

    private static void WriteSeparator(StreamWriter writer, int[] widths) {
        writer.Write("| ");
        for (int i = 0; i < widths.Length; i++) {
            if (i > 0) {
                writer.Write(" | ");
            }

            writer.Write(new string('-', Math.Max(3, widths[i])));
        }
        writer.WriteLine(" |");
    }

    private static void WriteRow(StreamWriter writer, IReadOnlyList<string> row, int[] widths) {
        writer.Write("| ");
        for (int i = 0; i < row.Count; i++) {
            if (i > 0) {
                writer.Write(" | ");
            }

            writer.Write(row[i].PadRight(widths[i]));
        }
        writer.WriteLine(" |");
    }

    private sealed record ComparisonGroup(string Label, List<string[]> Rows);
}