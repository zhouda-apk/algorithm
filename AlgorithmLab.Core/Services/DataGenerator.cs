using System;
using System.Security.Cryptography;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public sealed class DataGenerator
    {
        public GeneratedData Generate(DataDistribution distribution, int count, int minValue, int maxValue, int? seed = null)
        {
            CaseRules.Require(Enum.IsDefined(typeof(DataDistribution), distribution) && distribution != DataDistribution.Manual, "請選自動生成分布。");
            CaseRules.Require(count >= 1 && count <= 5000, "資料數量須為 1～5000。");
            CaseRules.Require(minValue >= -1000000 && maxValue <= 1000000 && minValue <= maxValue, "生成範圍須在 -1000000～1000000 且最小值不大於最大值。");
            int chosen;
            if (seed.HasValue) chosen = seed.Value;
            else { var bytes = new byte[4]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes); chosen = BitConverter.ToInt32(bytes, 0); }
            var random = new Random(chosen); var data = new int[count];
            int upper = distribution == DataDistribution.FewUnique ? minValue + Math.Min(10, maxValue - minValue + 1) : maxValue + 1;
            for (int i = 0; i < count; i++) data[i] = random.Next(minValue, upper);
            if (distribution == DataDistribution.Sorted || distribution == DataDistribution.Reversed) Array.Sort(data);
            if (distribution == DataDistribution.Reversed) Array.Reverse(data);
            return new GeneratedData(distribution, data, chosen, minValue, maxValue);
        }
    }
}
