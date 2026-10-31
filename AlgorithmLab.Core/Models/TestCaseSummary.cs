using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class TestCaseSummary
    {
        public Guid Id { get; }
        public string Name { get; }
        public int Revision { get; }
        public DataDistribution Distribution { get; }
        public int DataCount { get; }
        public DateTime UpdatedAtUtc { get; }
        public TestCaseSummary(TestCase value)
        { Id = value.Id; Name = value.Name; Revision = value.Revision; Distribution = value.Distribution; DataCount = value.DataCount; UpdatedAtUtc = value.UpdatedAtUtc; }
    }
}
