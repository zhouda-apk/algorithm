using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class AlgorithmDescriptor
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string TimeComplexityDescription { get; }
        public bool IsStable { get; }
        public bool IsMidtermAlgorithm { get; }
        public AlgorithmDescriptor(string id, string name, string version, string timeComplexityDescription, bool isStable, bool isMidtermAlgorithm)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(timeComplexityDescription))
                throw new ArgumentException("Algorithm descriptor fields are required; Id must not have surrounding spaces.");
            Id = id; Name = name; Version = version; TimeComplexityDescription = timeComplexityDescription;
            IsStable = isStable; IsMidtermAlgorithm = isMidtermAlgorithm;
        }
    }
}
