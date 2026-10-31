using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlgorithmLab.Data.FileStorage
{
    [DataContract]
    public sealed class StoreDocument
    {
        [DataMember(Name = "fileFormatVersion", IsRequired = true)]
        public int FileFormatVersion { get; set; }
        [DataMember(Name = "documentRevision", IsRequired = true)]
        public long DocumentRevision { get; set; }
        [DataMember(Name = "savedAtUtc", IsRequired = true)]
        public string SavedAtUtc { get; set; }
        [DataMember(Name = "testCases", IsRequired = true)]
        public List<TestCaseDto> TestCases { get; set; }
        [DataMember(Name = "benchmarkRuns", IsRequired = true)]
        public List<BenchmarkRunDto> BenchmarkRuns { get; set; }
        public static StoreDocument CreateEmpty()
        {
            return new StoreDocument { FileFormatVersion = 1, DocumentRevision = 1,
                SavedAtUtc = System.DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                TestCases = new List<TestCaseDto>(), BenchmarkRuns = new List<BenchmarkRunDto>() };
        }
    }
}
