using System.Linq;
using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class EnvironmentInfo
    {
        public string AppVersion { get; }
        public string BuildConfiguration { get; }
        public string ProcessArchitecture { get; }
        public string ClrVersion { get; }
        public string OsVersion { get; }
        public string MachineName { get; }
        public string CpuModel { get; }
        public string EnvironmentKey { get; }
        public int ProcessorCount { get; }
        public bool DebuggerAttached { get; }
        public bool IsHighResolution { get; }
        public long StopwatchFrequency { get; }
        public EnvironmentInfo(string appVersion, string buildConfiguration, string processArchitecture, string clrVersion, string osVersion, string machineName, string cpuModel, string environmentKey, int processorCount, bool debuggerAttached, bool isHighResolution, long stopwatchFrequency)
        {
            CaseRules.Require(stopwatchFrequency > 0 && processorCount > 0 && new[] { appVersion, buildConfiguration, processArchitecture, clrVersion, osVersion, machineName, cpuModel, environmentKey }.All(s => !string.IsNullOrWhiteSpace(s)), "Invalid environment metadata.");
            AppVersion = appVersion;
            BuildConfiguration = buildConfiguration;
            ProcessArchitecture = processArchitecture;
            ClrVersion = clrVersion;
            OsVersion = osVersion;
            MachineName = machineName;
            CpuModel = cpuModel;
            EnvironmentKey = environmentKey;
            ProcessorCount = processorCount;
            DebuggerAttached = debuggerAttached;
            IsHighResolution = isHighResolution;
            StopwatchFrequency = stopwatchFrequency;
        }
    }
}
