using System.Linq;
namespace AlgorithmLab.Core.Models
{
    public sealed class TestCaseDraft
    {
        private readonly int[] data;
        public string Name { get; }
        public DataDistribution Distribution { get; }
        public int[] Data { get { return data == null ? null : (int[])data.Clone(); } }
        public int? Seed { get; }
        public string GeneratorVersion { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public TestCaseDraft(string name, DataDistribution distribution, int[] data, int? seed, string generatorVersion, int minValue, int maxValue)
        {
            Name = name; Distribution = distribution; this.data = data == null ? null : (int[])data.Clone();
            Seed = seed; GeneratorVersion = generatorVersion; MinValue = minValue; MaxValue = maxValue;
        }
        public static TestCaseDraft Manual(string name, int[] data)
        {
            CaseRules.Require(data != null && data.Length >= 1 && data.Length <= 5000, "資料數量須為 1～5000。");
            return new TestCaseDraft(name, DataDistribution.Manual, data, null, null, data.Min(), data.Max());
        }
    }
}
