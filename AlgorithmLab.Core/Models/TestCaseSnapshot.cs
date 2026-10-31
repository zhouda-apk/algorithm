using System;
using AlgorithmLab.Core.Services;
namespace AlgorithmLab.Core.Models
{
    public sealed class TestCaseSnapshot
    {
        private readonly int[] data;
        public Guid? TestCaseId { get; }
        public int? CaseRevision { get; }
        public string CaseName { get; }
        public DataDistribution Distribution { get; }
        public int DataCount { get { return data.Length; } }
        public int[] Data { get { return (int[])data.Clone(); } }
        public int? Seed { get; }
        public string GeneratorVersion { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public string InputDataText { get; }
        public string InputFormatVersion { get { return "intcsv-1"; } }
        public string InputHash { get; }
        public TestCaseSnapshot(TestCase value) : this(
            value == null ? throw new ArgumentNullException(nameof(value)) : (Guid?)value.Id,
            value.Revision, value.Name, value.Distribution, value.Data, value.Seed, value.GeneratorVersion, value.MinValue, value.MaxValue) { }
        public TestCaseSnapshot(Guid? testCaseId, int? caseRevision, string caseName, DataDistribution distribution, int[] data, int? seed, string generatorVersion, int minValue, int maxValue)
        {
            var draft = new TestCaseDraft(caseName, distribution, data, seed, generatorVersion, minValue, maxValue);
            CaseRules.Validate(draft);
            CaseRules.Require((!testCaseId.HasValue || (testCaseId != Guid.Empty && caseRevision.HasValue)) && (!caseRevision.HasValue || caseRevision > 0), "Invalid snapshot identity.");
            TestCaseId = testCaseId; CaseRevision = caseRevision; CaseName = CaseRules.Name(caseName); Distribution = distribution;
            this.data = draft.Data; Seed = seed; GeneratorVersion = generatorVersion; MinValue = minValue; MaxValue = maxValue;
            InputDataText = IntArrayCodec.Encode(this.data); InputHash = IntArrayCodec.ComputeHash(InputDataText);
        }
    }
}
