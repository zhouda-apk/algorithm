using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public static class IntArrayCodec
    {
        public static string Encode(int[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return string.Join(",", data.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        }
        public static string ComputeHash(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
        public static int[] Decode(string text, int expectedCount, string inputHash)
        {
            if (text == null || expectedCount < 1 || expectedCount > 10000 || ComputeHash(text) != inputHash)
                throw new AlgorithmLabException(ErrorKind.DataIntegrity, "快照格式或雜湊不符。");
            var tokens = text.Split(',');
            if (tokens.Length != expectedCount) throw new AlgorithmLabException(ErrorKind.DataIntegrity, "快照筆數不符。");
            var values = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                if (!int.TryParse(tokens[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out values[i]) || values[i].ToString(CultureInfo.InvariantCulture) != tokens[i])
                    throw new AlgorithmLabException(ErrorKind.DataIntegrity, "快照整數格式不符。");
            return values;
        }
    }
}
