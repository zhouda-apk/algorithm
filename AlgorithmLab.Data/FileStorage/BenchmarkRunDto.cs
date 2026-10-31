using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlgorithmLab.Data.FileStorage
{
    [DataContract]
    public sealed class BenchmarkRunDto
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string Id { get; set; }
        [DataMember(Name = "testCaseId", IsRequired = true)]
        public string TestCaseId { get; set; }
        [DataMember(Name = "caseName", IsRequired = true)]
        public string CaseName { get; set; }
        [DataMember(Name = "distribution", IsRequired = true)]
        public string Distribution { get; set; }
        [DataMember(Name = "generatorVersion", IsRequired = true)]
        public string GeneratorVersion { get; set; }
        [DataMember(Name = "inputDataText", IsRequired = true)]
        public string InputDataText { get; set; }
        [DataMember(Name = "inputFormatVersion", IsRequired = true)]
        public string InputFormatVersion { get; set; }
        [DataMember(Name = "inputHash", IsRequired = true)]
        public string InputHash { get; set; }
        [DataMember(Name = "algorithmId", IsRequired = true)]
        public string AlgorithmId { get; set; }
        [DataMember(Name = "algorithmVersion", IsRequired = true)]
        public string AlgorithmVersion { get; set; }
        [DataMember(Name = "metricsVersion", IsRequired = true)]
        public string MetricsVersion { get; set; }
        [DataMember(Name = "status", IsRequired = true)]
        public string Status { get; set; }
        [DataMember(Name = "startedAtUtc", IsRequired = true)]
        public string StartedAtUtc { get; set; }
        [DataMember(Name = "finishedAtUtc", IsRequired = true)]
        public string FinishedAtUtc { get; set; }
        [DataMember(Name = "outputPreviewText", IsRequired = true)]
        public string OutputPreviewText { get; set; }
        [DataMember(Name = "appVersion", IsRequired = true)]
        public string AppVersion { get; set; }
        [DataMember(Name = "buildConfiguration", IsRequired = true)]
        public string BuildConfiguration { get; set; }
        [DataMember(Name = "processArchitecture", IsRequired = true)]
        public string ProcessArchitecture { get; set; }
        [DataMember(Name = "clrVersion", IsRequired = true)]
        public string ClrVersion { get; set; }
        [DataMember(Name = "osVersion", IsRequired = true)]
        public string OsVersion { get; set; }
        [DataMember(Name = "machineName", IsRequired = true)]
        public string MachineName { get; set; }
        [DataMember(Name = "cpuModel", IsRequired = true)]
        public string CpuModel { get; set; }
        [DataMember(Name = "environmentKey", IsRequired = true)]
        public string EnvironmentKey { get; set; }
        [DataMember(Name = "errorCode", IsRequired = true)]
        public string ErrorCode { get; set; }
        [DataMember(Name = "errorMessage", IsRequired = true)]
        public string ErrorMessage { get; set; }
        [DataMember(Name = "dataCount", IsRequired = true)]
        public int DataCount { get; set; }
        [DataMember(Name = "minValue", IsRequired = true)]
        public int MinValue { get; set; }
        [DataMember(Name = "maxValue", IsRequired = true)]
        public int MaxValue { get; set; }
        [DataMember(Name = "warmupCount", IsRequired = true)]
        public int WarmupCount { get; set; }
        [DataMember(Name = "requestedSampleCount", IsRequired = true)]
        public int RequestedSampleCount { get; set; }
        [DataMember(Name = "timeoutSeconds", IsRequired = true)]
        public int TimeoutSeconds { get; set; }
        [DataMember(Name = "processorCount", IsRequired = true)]
        public int ProcessorCount { get; set; }
        [DataMember(Name = "caseRevision", IsRequired = true)]
        public int? CaseRevision { get; set; }
        [DataMember(Name = "seed", IsRequired = true)]
        public int? Seed { get; set; }
        [DataMember(Name = "stopwatchFrequency", IsRequired = true)]
        public long StopwatchFrequency { get; set; }
        [DataMember(Name = "isHighResolution", IsRequired = true)]
        public bool IsHighResolution { get; set; }
        [DataMember(Name = "debuggerAttached", IsRequired = true)]
        public bool DebuggerAttached { get; set; }
        [DataMember(Name = "medianMs", IsRequired = true)]
        public double? MedianMs { get; set; }
        [DataMember(Name = "meanMs", IsRequired = true)]
        public double? MeanMs { get; set; }
        [DataMember(Name = "minMs", IsRequired = true)]
        public double? MinMs { get; set; }
        [DataMember(Name = "maxMs", IsRequired = true)]
        public double? MaxMs { get; set; }
        [DataMember(Name = "populationStdDevMs", IsRequired = true)]
        public double? PopulationStdDevMs { get; set; }
        [DataMember(Name = "comparisonCount", IsRequired = true)]
        public long? ComparisonCount { get; set; }
        [DataMember(Name = "swapCount", IsRequired = true)]
        public long? SwapCount { get; set; }
        [DataMember(Name = "writeCount", IsRequired = true)]
        public long? WriteCount { get; set; }
        [DataMember(Name = "samples", IsRequired = true)]
        public List<BenchmarkSampleDto> Samples { get; set; }
    }
}
