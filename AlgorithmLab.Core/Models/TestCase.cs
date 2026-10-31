using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class TestCase
    {
        private readonly int[] data;
        public Guid Id { get; }
        public string Name { get; }
        public int Revision { get; }
        public DataDistribution Distribution { get; }
        public int DataCount { get { return data.Length; } }
        public int[] Data { get { return (int[])data.Clone(); } }
        public int? Seed { get; }
        public string GeneratorVersion { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public DateTime CreatedAtUtc { get; }
        public DateTime UpdatedAtUtc { get; }
        public TestCase(Guid id, int revision, TestCaseDraft draft, DateTime createdAtUtc, DateTime updatedAtUtc)
        {
            CaseRules.Validate(draft);
            CaseRules.Require(id != Guid.Empty && revision > 0, "案例 ID 或版次無效。");
            CaseRules.Require(createdAtUtc.Kind == DateTimeKind.Utc && updatedAtUtc.Kind == DateTimeKind.Utc && updatedAtUtc >= createdAtUtc, "案例時間必須為有效 UTC。");
            Id = id; Revision = revision; Name = CaseRules.Name(draft.Name); Distribution = draft.Distribution; data = draft.Data;
            Seed = draft.Seed; GeneratorVersion = draft.GeneratorVersion; MinValue = draft.MinValue; MaxValue = draft.MaxValue;
            CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
        }
        public TestCaseDraft ToDraft(string name)
        { return new TestCaseDraft(name, Distribution, data, Seed, GeneratorVersion, MinValue, MaxValue); }
    }
}
