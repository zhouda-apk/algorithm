using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkHistoryQuery
    {
        public Guid? TestCaseId { get; }
        public string AlgorithmId { get; }
        public BenchmarkStatus? Status { get; }
        public int Offset { get; }
        public int Limit { get; }
        public BenchmarkHistoryQuery(Guid? testCaseId = null, string algorithmId = null, BenchmarkStatus? status = null, int offset = 0, int limit = 20)
        {
            CaseRules.Require(offset >= 0 && limit >= 1 && limit <= 100 && (!status.HasValue || Enum.IsDefined(typeof(BenchmarkStatus), status.Value)), "Invalid history query.");
            TestCaseId = testCaseId;
            AlgorithmId = algorithmId;
            Status = status;
            Offset = offset;
            Limit = limit;
        }
    }
}
