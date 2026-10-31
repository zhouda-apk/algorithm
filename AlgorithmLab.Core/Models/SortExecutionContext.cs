using System;
using System.Threading;
namespace AlgorithmLab.Core.Models
{
    public sealed class SortExecutionContext
    {
        private int used;
        public SortExecutionMode Mode { get; }
        public CancellationToken CancellationToken { get; }
        public SortMetrics Metrics { get; }
        public SortExecutionContext(SortExecutionMode mode, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Enum.IsDefined(typeof(SortExecutionMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            Mode = mode; CancellationToken = cancellationToken; Metrics = new SortMetrics();
        }
        internal void BeginSort()
        {
            if (Interlocked.CompareExchange(ref used, 1, 0) != 0) throw new InvalidOperationException("Create a fresh SortExecutionContext for every Sort call.");
        }
    }
}
