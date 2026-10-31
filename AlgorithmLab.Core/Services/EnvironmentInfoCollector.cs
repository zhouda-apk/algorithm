using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public static class EnvironmentInfoCollector
    {
        public static EnvironmentInfo Collect()
        {
#if DEBUG
            const string build = "Debug";
#else
            const string build = "Release";
#endif
            string version = typeof(EnvironmentInfoCollector).Assembly.GetName().Version.ToString();
            string architecture = Environment.Is64BitProcess ? "x64" : "x86";
            string cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"); if (string.IsNullOrWhiteSpace(cpu)) cpu = "Unknown";
            var fields = new[] { version, build, architecture, Environment.Version.ToString(), Environment.OSVersion.VersionString, Environment.MachineName,
                Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture), cpu, Debugger.IsAttached ? "1" : "0", Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture), Stopwatch.IsHighResolution ? "1" : "0" };
            string key = IntArrayCodec.ComputeHash(string.Concat(fields.Select(f => f.Length.ToString(CultureInfo.InvariantCulture) + ":" + f)));
            return new EnvironmentInfo(version, build, architecture, fields[3], fields[4], fields[5], cpu, key, Environment.ProcessorCount, Debugger.IsAttached, Stopwatch.IsHighResolution, Stopwatch.Frequency);
        }
    }
}
