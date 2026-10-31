using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AlgorithmLab.Core.Models;

namespace AlgorithmLab.Data.FileStorage
{
    internal static class DocumentValidator
    {
        internal static void Require(bool condition, string message)
        {
            if (!condition) throw new AlgorithmLabException(ErrorKind.DataIntegrity, message);
        }
        private static void Id(string value) { Guid id; Require(Guid.TryParseExact(value, "D", out id) && id != Guid.Empty, "Invalid GUID."); }
        private static void Name(string value) { Require(value != null && value.Length >= 1 && value.Length <= 50 && value == value.Trim() && !value.Any(char.IsControl), "Invalid case name."); }
        private static DateTime Utc(string value)
        {
            DateTime time;
            Require(DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out time) && time.Kind == DateTimeKind.Utc, "Invalid UTC timestamp.");
            return time;
        }
        internal static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
        private static void Data(string distribution, int? seed, string version, int min, int max, int[] data)
        {
            Require(data != null && data.Length >= 1 && data.Length <= 10000, "Invalid data length.");
            Require(new[] { "Random", "Sorted", "Reversed", "FewUnique", "Manual" }.Contains(distribution), "Unknown distribution.");
            Require(min <= max && data.All(v => v >= min && v <= max), "Data outside declared range.");
            if (distribution == "Manual") Require(seed == null && version == null && data.Min() == min && data.Max() == max, "Invalid manual metadata.");
            else Require(seed.HasValue && version == "gen-1" && min >= -1000000 && max <= 1000000, "Invalid generator metadata.");
            if (distribution == "Sorted") Require(data.Zip(data.Skip(1), (a, b) => a <= b).All(x => x), "Sorted data is not sorted.");
            if (distribution == "Reversed") Require(data.Zip(data.Skip(1), (a, b) => a >= b).All(x => x), "Reversed data is not reversed.");
            if (distribution == "FewUnique") Require(data.All(v => (long)v - min < 10), "Invalid FewUnique values.");
        }
        internal static void Validate(StoreDocument document)
        {
            Require(document != null && document.FileFormatVersion == 1, "Unsupported or missing fileFormatVersion.");
            Require(document.DocumentRevision > 0 && document.DocumentRevision < long.MaxValue, "Invalid documentRevision."); Utc(document.SavedAtUtc);
            Require(document.TestCases != null && document.BenchmarkRuns != null, "Missing collections.");
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in document.TestCases)
            {
                Require(c != null, "Null case."); Id(c.Id); Require(ids.Add(c.Id), "Duplicate case ID."); Name(c.Name);
                Require(c.Revision > 0 && c.Data != null && c.DataCount == c.Data.Length && c.DataCount <= 5000, "Invalid case count or revision.");
                Require(Utc(c.UpdatedAtUtc) >= Utc(c.CreatedAtUtc), "Invalid case timestamp order.");
                Data(c.Distribution, c.Seed, c.GeneratorVersion, c.MinValue, c.MaxValue, c.Data);
            }
            var runs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in document.BenchmarkRuns)
            {
                Require(r != null, "Null run."); Id(r.Id); Require(runs.Add(r.Id), "Duplicate run ID."); Name(r.CaseName);
                if (r.TestCaseId != null) { Id(r.TestCaseId); Require(ids.Contains(r.TestCaseId) && r.CaseRevision.HasValue, "Orphan case reference."); }
                Require(!r.CaseRevision.HasValue || r.CaseRevision > 0, "Invalid case revision.");
                Require(r.InputFormatVersion == "intcsv-1" && r.InputDataText != null, "Invalid snapshot format.");
                var tokens = r.InputDataText.Split(',');
                Require(tokens.Length == r.DataCount && tokens.Length <= 10000, "Snapshot length mismatch.");
                var values = new int[tokens.Length];
                for (int i = 0; i < tokens.Length; i++)
                    Require(int.TryParse(tokens[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out values[i]) && values[i].ToString(CultureInfo.InvariantCulture) == tokens[i], "Noncanonical snapshot integer.");
                Require(r.InputHash == Hash(Encoding.UTF8.GetBytes(r.InputDataText)), "Snapshot hash mismatch.");
                Data(r.Distribution, r.Seed, r.GeneratorVersion, r.MinValue, r.MaxValue, values);
                Require(new[] { "bubble", "selection", "insertion", "quick", "merge" }.Contains(r.AlgorithmId) && !string.IsNullOrWhiteSpace(r.AlgorithmVersion) && r.MetricsVersion == "metrics-1", "Invalid algorithm metadata.");
                Require((r.WarmupCount == 1 || r.WarmupCount == 3) && r.RequestedSampleCount >= 5 && r.RequestedSampleCount <= 30 && r.TimeoutSeconds >= 10 && r.TimeoutSeconds <= 120, "Invalid benchmark settings.");
                Require(new[] { "Running", "Completed", "Cancelled", "Failed", "Interrupted" }.Contains(r.Status), "Unknown run status.");
                var started = Utc(r.StartedAtUtc);
                if (r.Status == "Running") Require(r.FinishedAtUtc == null, "Running has finish timestamp.");
                else Require(Utc(r.FinishedAtUtc) >= started, "Invalid finish timestamp.");
                Require(r.StopwatchFrequency > 0 && r.ProcessorCount > 0 && r.Samples != null, "Invalid environment or samples.");
                foreach (var text in new[] { r.AppVersion, r.ClrVersion, r.OsVersion, r.MachineName, r.CpuModel }) Require(!string.IsNullOrWhiteSpace(text), "Missing environment metadata.");
                Require(new[] { "Debug", "Release" }.Contains(r.BuildConfiguration) && new[] { "x64", "x86" }.Contains(r.ProcessArchitecture), "Invalid build metadata.");
                Require(r.EnvironmentKey != null && r.EnvironmentKey.Length == 64 && r.EnvironmentKey.All(c => "0123456789abcdef".Contains(c)), "Invalid environment key.");
                Require(r.Samples.Count <= r.RequestedSampleCount, "Too many samples.");
                for (int i = 0; i < r.Samples.Count; i++) Require(r.Samples[i] != null && r.Samples[i].SampleIndex == i + 1 && r.Samples[i].ElapsedTimerTicks >= 0, "Invalid sample sequence.");
                var stats = new[] { r.MedianMs, r.MeanMs, r.MinMs, r.MaxMs, r.PopulationStdDevMs };
                var counts = new[] { r.ComparisonCount, r.SwapCount, r.WriteCount };
                Require(counts.All(v => v == null) || counts.All(v => v.HasValue && v.Value >= 0), "Incomplete or negative operation counts.");
                if (r.Status == "Completed")
                {
                    Require(r.Samples.Count == r.RequestedSampleCount && r.Samples.All(s => s.IsValid), "Completed run has invalid samples.");
                    Require(stats.All(v => v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value) && v.Value >= 0) && counts.All(v => v.HasValue), "Completed run lacks statistics.");
                    Require(r.MinMs <= r.MedianMs && r.MedianMs <= r.MaxMs && r.MinMs <= r.MeanMs && r.MeanMs <= r.MaxMs && r.OutputPreviewText != null && r.ErrorCode == null && r.ErrorMessage == null, "Inconsistent completion metadata.");
                }
                else Require(stats.All(v => v == null), "Incomplete run has aggregate statistics.");
                if (r.Status == "Running") Require(r.Samples.Count == 0 && counts.All(v => v == null), "Running has final data.");
            }
        }
    }
}
