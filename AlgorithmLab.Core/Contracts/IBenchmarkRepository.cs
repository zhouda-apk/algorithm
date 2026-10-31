using System;
using System.Collections.Generic;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Contracts
{
    public interface IBenchmarkRepository
    {
        void CreateRunning(BenchmarkRun run);
        BenchmarkRun FinalizeRun(PendingRunFinalization finalization);
        IReadOnlyList<BenchmarkRun> GetHistory(BenchmarkHistoryQuery query);
        BenchmarkRun GetDetails(Guid runId);
        int MarkInterruptedRuns(DateTime utcNow);
    }
}
