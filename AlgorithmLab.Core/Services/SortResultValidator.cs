using System;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public static class SortResultValidator
    {
        public static int[] CreateExpected(int[] original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var result = (int[])original.Clone(); Array.Sort(result); return result;
        }
        public static void Validate(int[] expected, int[] actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (expected.Length != actual.Length) throw new AlgorithmLabException(ErrorKind.SortValidationFailure, "Sorted output length differs from the expected answer.");
            for (int i = 0; i < expected.Length; i++)
            {
                if (i > 0 && expected[i - 1] > expected[i]) throw new ArgumentException("Expected answer must be sorted.", nameof(expected));
                if (expected[i] != actual[i]) throw new AlgorithmLabException(ErrorKind.SortValidationFailure, "Sorted output differs at index " + i + ".");
            }
        }
    }
}
