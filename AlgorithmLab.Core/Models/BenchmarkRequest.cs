using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkRequest
    {
        public TestCaseSnapshot Input { get; }
        public string AlgorithmId { get; }
        public BenchmarkOptions Options { get; }
        public BenchmarkRequest(TestCaseSnapshot input, string algorithmId, BenchmarkOptions options)
        {
            if (input == null || options == null) throw new ArgumentNullException(nameof(input));
            CaseRules.Require(!string.IsNullOrWhiteSpace(algorithmId), "Algorithm Id is required.");
            Input = input;
            AlgorithmId = algorithmId;
            Options = options;
        }
    }
}
