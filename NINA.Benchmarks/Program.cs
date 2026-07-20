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
using BenchmarkDotNet.Jobs;
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
    private static readonly string[] Headers = ["Thread Count", "Method", "Mean", "Error", "StdDev", "Median", "Ratio", "RatioSD", "Allocated", "Alloc Ratio"];

    public static string WriteComparisonSummary(IEnumerable<Summary> summaries, string resultsDirectoryPath, IEnumerable<string> args) {
        Directory.CreateDirectory(resultsDirectoryPath);
        string resultsPath = Path.Combine(resultsDirectoryPath, ComparisonResultsFileName);
        string commandLine = string.Join(" ", args);

        using var writer = new StreamWriter(resultsPath, false, new UTF8Encoding(false));

        writer.WriteLine("NINA Benchmark Comparisons");
        writer.WriteLine($"Generated: {DateTimeOffset.Now:O}");
        writer.WriteLine($"Command: {(string.IsNullOrWhiteSpace(commandLine) ? "<none>" : commandLine)}");
        writer.WriteLine();

        var groupsByTitle = CreateGroups(summaries)
            .GroupBy(group => group.Title)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToList();

        if (groupsByTitle.Count == 0) {
            writer.WriteLine("No benchmarks were executed.");
            return resultsPath;
        }

        foreach (var titleGroup in groupsByTitle) {
            writer.WriteLine(titleGroup.Key);
            writer.WriteLine(new string('=', titleGroup.Key.Length));
            writer.WriteLine();

            foreach (ComparisonGroup group in titleGroup.OrderBy(group => group.Label, StringComparer.Ordinal)) {
                writer.WriteLine(group.Label);
                writer.WriteLine();
                WriteTable(writer, group.Rows);
                writer.WriteLine();
            }
        }

        return resultsPath;
    }

    private static IEnumerable<ComparisonGroup> CreateGroups(IEnumerable<Summary> summaries) {
        return summaries
            .SelectMany(summary => summary.Reports)
            .Where(report => report.Success && report.ResultStatistics != null)
            .GroupBy(report => new {
                Title = report.BenchmarkCase.Descriptor.Type.Name,
                Label = GetGroupKey(report.BenchmarkCase)
            })
            .Select(group => CreateGroup(group.Key.Title, group.Key.Label, group.ToList()));
    }

    private static ComparisonGroup CreateGroup(string title, string label, List<BenchmarkReport> reports) {
        var rows = new List<string[]>();

        foreach (var threadGroup in reports
            .GroupBy(report => GetThreadCountLabel(report.BenchmarkCase.Job))
            .OrderBy(group => GetThreadCountSortKey(group.Key))) {
            List<BenchmarkReport> orderedReports = threadGroup
                .OrderByDescending(report => report.BenchmarkCase.Descriptor.Baseline)
                .ThenBy(report => report.BenchmarkCase.Descriptor.WorkloadMethod.Name, StringComparer.Ordinal)
                .ToList();

            BenchmarkReport baseline = orderedReports.FirstOrDefault(report => report.BenchmarkCase.Descriptor.Baseline) ?? orderedReports[0];
            double baselineMean = baseline.ResultStatistics!.Mean;
            double baselineStdDev = baseline.ResultStatistics.StandardDeviation;
            long? baselineAllocated = baseline.GcStats.GetBytesAllocatedPerOperation(baseline.BenchmarkCase);

            rows.AddRange(orderedReports.Select(report => CreateRow(report, threadGroup.Key, baseline, baselineMean, baselineStdDev, baselineAllocated)));
        }

        return new ComparisonGroup(title, label, rows);
    }

    private static string[] CreateRow(BenchmarkReport report, string threadCountLabel, BenchmarkReport baseline, double baselineMean, double baselineStdDev, long? baselineAllocated) {
        var stats = report.ResultStatistics!;
        bool isBaseline = ReferenceEquals(report, baseline);
        long? allocated = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);

        double ratio = baselineMean == 0 ? 0 : stats.Mean / baselineMean;
        double? ratioSd = isBaseline ? null : CalculateRatioStdDev(stats.Mean, stats.StandardDeviation, baselineMean, baselineStdDev, ratio);
        double? allocRatio = (!baselineAllocated.HasValue || baselineAllocated.Value == 0 || !allocated.HasValue)
            ? null
            : allocated.Value / (double)baselineAllocated.Value;

        return [
            threadCountLabel,
            report.BenchmarkCase.Descriptor.WorkloadMethod.Name,
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

    private static string GetThreadCountLabel(Job job) {
        if (!string.IsNullOrWhiteSpace(job.ResolvedId)) {
            return job.ResolvedId;
        }

        if (!string.IsNullOrWhiteSpace(job.Id)) {
            return job.Id;
        }

        return job.DisplayInfo;
    }

    private static int GetThreadCountSortKey(string threadCountLabel) {
        return threadCountLabel == "1" ? 0 : 1;
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

    private sealed record ComparisonGroup(string Title, string Label, List<string[]> Rows);
}