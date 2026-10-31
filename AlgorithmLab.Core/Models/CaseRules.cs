using System;
using System.Linq;
namespace AlgorithmLab.Core.Models
{
    internal static class CaseRules
    {
        internal static void Require(bool value, string message)
        { if (!value) throw new AlgorithmLabException(ErrorKind.Validation, message); }
        internal static string Name(string name)
        {
            name = name == null ? null : name.Trim();
            Require(name != null && name.Length >= 1 && name.Length <= 50 && !name.Any(char.IsControl), "案例名稱須為 1～50 字元，且不能含控制字元。");
            return name;
        }
        internal static void Validate(TestCaseDraft draft)
        {
            Require(draft != null, "缺少案例資料。"); Name(draft.Name);
            var data = draft.Data;
            Require(data != null && data.Length >= 1 && data.Length <= 5000, "資料數量須為 1～5000。");
            Require(Enum.IsDefined(typeof(DataDistribution), draft.Distribution), "未知資料分布。");
            Require(draft.MinValue <= draft.MaxValue && data.All(v => v >= draft.MinValue && v <= draft.MaxValue), "數值與設定範圍不符。");
            if (draft.Distribution == DataDistribution.Manual)
                Require(draft.Seed == null && draft.GeneratorVersion == null && data.Min() == draft.MinValue && data.Max() == draft.MaxValue, "手動資料的種子、版本或極值不符。");
            else
                Require(draft.Seed.HasValue && draft.GeneratorVersion == "gen-1" && draft.MinValue >= -1000000 && draft.MaxValue <= 1000000, "自動生成參數不符。");
            if (draft.Distribution == DataDistribution.Sorted) Require(data.SequenceEqual(data.OrderBy(v => v)), "Sorted 資料必須遞增排列。");
            if (draft.Distribution == DataDistribution.Reversed) Require(data.SequenceEqual(data.OrderByDescending(v => v)), "Reversed 資料必須遞減排列。");
            if (draft.Distribution == DataDistribution.FewUnique) Require(data.All(v => (long)v - draft.MinValue < 10), "FewUnique 超出候選範圍。");
        }
    }
}
