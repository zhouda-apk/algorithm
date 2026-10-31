using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Algorithms
{
    public sealed class BubbleSort : SortAlgorithmBase
    {
        private static readonly AlgorithmDescriptor Metadata = new AlgorithmDescriptor("bubble", "Bubble Sort", "1.0", "Best O(n); average/worst O(n^2)", true, true);
        public override AlgorithmDescriptor Descriptor { get { return Metadata; } }
        protected override void SortCore(int[] data, SortExecutionContext context)
        {
            for (int end = data.Length - 1; end > 0; end--)
            {
                CheckCancellation(context); bool changed = false;
                for (int i = 0; i < end; i++)
                {
                    if (CompareValues(data[i], data[i + 1], context) > 0)
                    { SwapValues(data, i, i + 1, context); changed = true; }
                }
                if (!changed) break;
            }
        }
    }
}
