using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkPersistenceException : AlgorithmLabException
    {
        public PendingRunFinalization PendingRunFinalization { get; }
        public BenchmarkPersistenceException(PendingRunFinalization pending, Exception inner) : base(ErrorKind.Persistence, "測試已結束，紀錄儲存失敗。可重試原結果保存。", inner)
        { PendingRunFinalization = pending ?? throw new ArgumentNullException(nameof(pending)); }
    }
}
