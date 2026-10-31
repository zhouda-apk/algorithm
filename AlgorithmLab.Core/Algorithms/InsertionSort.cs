using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Algorithms
{
    public sealed class InsertionSort : SortAlgorithmBase
    {
        private static readonly AlgorithmDescriptor Metadata = new AlgorithmDescriptor("insertion", "Insertion Sort", "1.0", "Best O(n); average/worst O(n^2)", true, true);
        public override AlgorithmDescriptor Descriptor { get { return Metadata; } }
        protected override void SortCore(int[] data, SortExecutionContext context)
        {
            for (int i = 1; i < data.Length; i++)
            {
                CheckCancellation(context); int key = data[i]; int j = i - 1;
                while (j >= 0 && CompareValues(data[j], key, context) > 0)
                { WriteValue(data, j + 1, data[j], context); j--; }
                WriteValue(data, j + 1, key, context);
            }
        }
    }
}
