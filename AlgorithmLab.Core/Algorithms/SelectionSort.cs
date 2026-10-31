using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Algorithms
{
    public sealed class SelectionSort : SortAlgorithmBase
    {
        private static readonly AlgorithmDescriptor Metadata = new AlgorithmDescriptor("selection", "Selection Sort", "1.0", "Best/average/worst O(n^2)", false, true);
        public override AlgorithmDescriptor Descriptor { get { return Metadata; } }
        protected override void SortCore(int[] data, SortExecutionContext context)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                CheckCancellation(context); int minimum = i;
                for (int j = i + 1; j < data.Length; j++)
                    if (CompareValues(data[j], data[minimum], context) < 0) minimum = j;
                if (minimum != i) SwapValues(data, i, minimum, context);
            }
        }
    }
}
