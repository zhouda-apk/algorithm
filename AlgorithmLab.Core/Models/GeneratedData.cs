namespace AlgorithmLab.Core.Models
{
    public sealed class GeneratedData
    {
        private readonly int[] data;
        public DataDistribution Distribution { get; }
        public int DataCount { get { return data.Length; } }
        public int[] Data { get { return (int[])data.Clone(); } }
        public int Seed { get; }
        public string GeneratorVersion { get { return "gen-1"; } }
        public int MinValue { get; }
        public int MaxValue { get; }
        internal GeneratedData(DataDistribution distribution, int[] data, int seed, int min, int max)
        { Distribution = distribution; this.data = (int[])data.Clone(); Seed = seed; MinValue = min; MaxValue = max; }
        public TestCaseDraft ToDraft(string name)
        { return new TestCaseDraft(name, Distribution, data, Seed, GeneratorVersion, MinValue, MaxValue); }
    }
}
