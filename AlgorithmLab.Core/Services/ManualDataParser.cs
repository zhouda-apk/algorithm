using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public static class ManualDataParser
    {
        public static int[] Parse(string text)
        {
            CaseRules.Require(!string.IsNullOrWhiteSpace(text), "請輸入至少一個整數。");
            var result = new List<int>();
            foreach (var segment in text.Split(','))
            {
                CaseRules.Require(!string.IsNullOrWhiteSpace(segment), "逗號之間或尾端有缺值。");
                foreach (var token in Regex.Split(segment.Trim(), @"\s+"))
                {
                    int value;
                    CaseRules.Require(int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value), "無效或超出 Int32 的整數：" + token);
                    result.Add(value); CaseRules.Require(result.Count <= 5000, "資料不能超過 5000 筆。");
                }
            }
            return result.ToArray();
        }
    }
}
