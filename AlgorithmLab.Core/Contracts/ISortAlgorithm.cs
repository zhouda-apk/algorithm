using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Contracts
{
    public interface ISortAlgorithm
    {
        AlgorithmDescriptor Descriptor { get; }
        void Sort(int[] data, SortExecutionContext context);
    }
}
