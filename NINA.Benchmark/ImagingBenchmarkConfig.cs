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
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Globalization;
using System.IO;
using System.Text;

namespace NINA.Benchmark;

public sealed class ImagingBenchmarkConfig : ManualConfig {
    internal static readonly string ProjectRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    public ImagingBenchmarkConfig() {
        ArtifactsPath = Path.Combine(ProjectRootPath, "BenchmarkDotNet.Artifacts");
        SummaryStyle = SummaryStyle.Default.WithMaxParameterColumnWidth(80);

        foreach (int workerCount in GetWorkerCounts(Environment.ProcessorCount)) {
            string workerCountLabel = workerCount.ToString(CultureInfo.InvariantCulture);
            // Production code uses ProcessorCount - 1, so expose one more logical processor
            // to each benchmark process than the number of workers being measured.
            string processorCount = (workerCount + 1).ToString(CultureInfo.InvariantCulture);

            AddJob(Job.Default
                .WithId(workerCountLabel)
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithIterationCount(3)
                .WithEnvironmentVariable("DOTNET_PROCESSOR_COUNT", processorCount)
                .WithEnvironmentVariable("COMPlus_ProcessorCount", processorCount));
        }

        AddExporter(new ImagingComparisonExporter());
    }

    internal static IEnumerable<int> GetWorkerCounts(int processorCount) {
        int maximumWorkerCount = Math.Max(1, processorCount - 1);
        return new SortedSet<int> { 1, (maximumWorkerCount + 1) / 2, maximumWorkerCount };
    }
}

internal sealed class ImagingComparisonExporter : IExporter {
    private const string ComparisonResultsFileName = "benchmark-results.txt";
    private static readonly string[] Headers = ["Thread Count", "Method", "Mean", "Error", "StdDev", "Median", "Ratio", "RatioSD", "Allocated", "Alloc Ratio"];
    private static readonly object WriteLock = new();
    private static readonly SortedDictionary<string, string> Sections = new(StringComparer.Ordinal);

    public string Name => nameof(ImagingComparisonExporter);

    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) {
        if (summary.BenchmarksCases.Length == 0) {
            return [];
        }

        string benchmarkName = summary.BenchmarksCases[0].Descriptor.Type.Name;
        string resultsDirectoryPath = Path.Combine(ImagingBenchmarkConfig.ProjectRootPath, "Results");
        string resultsPath = Path.Combine(resultsDirectoryPath, ComparisonResultsFileName);
        string section = CreateSection(benchmarkName, summary.Reports);

        lock (WriteLock) {
            Sections[benchmarkName] = section;
            Directory.CreateDirectory(resultsDirectoryPath);

            using var writer = new StreamWriter(resultsPath, false, new UTF8Encoding(false));
            writer.WriteLine("NINA Benchmark Comparisons");
            writer.WriteLine($"Generated: {DateTimeOffset.Now:O}");
            string commandLine = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
            writer.WriteLine($"Command: {(string.IsNullOrWhiteSpace(commandLine) ? "<none>" : commandLine)}");
            writer.WriteLine();

            foreach (string comparisonSection in Sections.Values) {
                writer.Write(comparisonSection);
            }
        }

        return [resultsPath];
    }

    public void ExportToLog(Summary summary, ILogger logger) {
    }

    private static string CreateSection(string benchmarkName, IEnumerable<BenchmarkReport> reports) {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine(benchmarkName);
        writer.WriteLine(new string('=', benchmarkName.Length));
        writer.WriteLine();

        var groups = reports
            .Where(report => report.Success && report.ResultStatistics != null)
            .GroupBy(report => GetGroupKey(report.BenchmarkCase))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();

        if (groups.Length == 0) {
            writer.WriteLine("No successful benchmark results.");
            writer.WriteLine();
        }

        foreach (var group in groups) {
            writer.WriteLine(group.Key);
            writer.WriteLine();

            var rows = group
                .GroupBy(report => GetThreadCountLabel(report.BenchmarkCase.Job))
                .OrderBy(threadGroup => GetThreadCountSortKey(threadGroup.Key))
                .ThenBy(threadGroup => threadGroup.Key, StringComparer.Ordinal)
                .SelectMany(threadGroup => CreateRows(threadGroup.Key, threadGroup))
                .ToArray();

            WriteTable(writer, rows);
            writer.WriteLine();
        }

        return writer.ToString();
    }

    private static IEnumerable<string[]> CreateRows(string threadCountLabel, IEnumerable<BenchmarkReport> reports) {
        var orderedReports = reports
            .OrderByDescending(report => report.BenchmarkCase.Descriptor.Baseline)
            .ThenBy(report => report.BenchmarkCase.Descriptor.WorkloadMethod.Name, StringComparer.Ordinal)
            .ToArray();
        BenchmarkReport baseline = orderedReports.FirstOrDefault(report => report.BenchmarkCase.Descriptor.Baseline) ?? orderedReports[0];
        double baselineMean = baseline.ResultStatistics!.Mean;
        double baselineStdDev = baseline.ResultStatistics.StandardDeviation;
        long? baselineAllocated = baseline.GcStats.GetBytesAllocatedPerOperation(baseline.BenchmarkCase);

        foreach (BenchmarkReport report in orderedReports) {
            var stats = report.ResultStatistics!;
            bool isBaseline = ReferenceEquals(report, baseline);
            long? allocated = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);
            double ratio = baselineMean == 0 ? 0 : stats.Mean / baselineMean;
            double? ratioSd = isBaseline ? null : CalculateRatioStdDev(stats.Mean, stats.StandardDeviation, baselineMean, baselineStdDev, ratio);
            double? allocRatio = (!baselineAllocated.HasValue || baselineAllocated.Value == 0 || !allocated.HasValue)
                ? null
                : allocated.Value / (double)baselineAllocated.Value;

            yield return [
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
        var groupItems = benchmarkCase.Parameters.Items
            .Where(item => !item.Name.Equals("threads", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (groupItems.Length == 0) {
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
        return int.TryParse(threadCountLabel, NumberStyles.None, CultureInfo.InvariantCulture, out int count) ? count : int.MaxValue;
    }

    private static string FormatDuration(double nanoseconds) {
        if (double.IsNaN(nanoseconds) || double.IsInfinity(nanoseconds)) {
            return "-";
        }

        if (Math.Abs(nanoseconds) >= 1_000_000) {
            return $"{(nanoseconds / 1_000_000).ToString("0.00", CultureInfo.InvariantCulture)} ms";
        }

        if (Math.Abs(nanoseconds) >= 1_000) {
            return $"{(nanoseconds / 1_000).ToString("0.00", CultureInfo.InvariantCulture)} us";
        }

        return $"{nanoseconds.ToString("0.00", CultureInfo.InvariantCulture)} ns";
    }

    private static string FormatBytes(long? bytes) {
        if (!bytes.HasValue) {
            return "-";
        }

        double value = bytes.Value;
        if (Math.Abs(value) >= 1024 * 1024) {
            return $"{(value / (1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture)} MB";
        }

        if (Math.Abs(value) >= 1024) {
            return $"{(value / 1024).ToString("0.00", CultureInfo.InvariantCulture)} KB";
        }

        return $"{value.ToString("0", CultureInfo.InvariantCulture)} B";
    }

    private static string FormatRatio(double? value) {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) {
            return "-";
        }

        return value.Value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static void WriteTable(TextWriter writer, IReadOnlyList<string[]> rows) {
        int[] widths = Headers.Select(header => header.Length).ToArray();
        foreach (string[] row in rows) {
            for (int i = 0; i < row.Length; i++) {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        WriteRow(writer, Headers, widths);
        writer.Write("| ");
        for (int i = 0; i < widths.Length; i++) {
            if (i > 0) {
                writer.Write(" | ");
            }

            writer.Write(new string('-', Math.Max(3, widths[i])));
        }
        writer.WriteLine(" |");

        foreach (string[] row in rows) {
            WriteRow(writer, row, widths);
        }
    }

    private static void WriteRow(TextWriter writer, IReadOnlyList<string> row, int[] widths) {
        writer.Write("| ");
        for (int i = 0; i < row.Count; i++) {
            if (i > 0) {
                writer.Write(" | ");
            }

            writer.Write(row[i].PadRight(widths[i]));
        }
        writer.WriteLine(" |");
    }
}
