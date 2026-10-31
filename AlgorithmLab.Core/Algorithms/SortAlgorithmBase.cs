using System;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Algorithms
{
    public abstract class SortAlgorithmBase : ISortAlgorithm
    {
        public abstract AlgorithmDescriptor Descriptor { get; }
        public void Sort(int[] data, SortExecutionContext context)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.BeginSort(); CheckCancellation(context);
            SortCore(data, context);
            CheckCancellation(context);
        }
        protected abstract void SortCore(int[] data, SortExecutionContext context);
        protected static void CheckCancellation(SortExecutionContext context) { context.CancellationToken.ThrowIfCancellationRequested(); }
        protected static int CompareValues(int left, int right, SortExecutionContext context)
        {
            CheckCancellation(context);
            if (context.Mode == SortExecutionMode.Analysis) context.Metrics.Compare();
            return left.CompareTo(right);
        }
        protected static void SwapValues(int[] data, int first, int second, SortExecutionContext context)
        {
            CheckCancellation(context);
            if (first == second) return;
            int temporary = data[first]; data[first] = data[second]; data[second] = temporary;
            if (context.Mode == SortExecutionMode.Analysis) context.Metrics.Swap();
        }
        protected static void WriteValue(int[] data, int index, int value, SortExecutionContext context)
        {
            CheckCancellation(context); data[index] = value;
            if (context.Mode == SortExecutionMode.Analysis) context.Metrics.Write();
        }
    }
}
