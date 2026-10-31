using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlgorithmLab.Data.FileStorage
{
    [DataContract]
    public sealed class BenchmarkSampleDto
    {
        [DataMember(Name = "sampleIndex", IsRequired = true)]
        public int SampleIndex { get; set; }
        [DataMember(Name = "elapsedTimerTicks", IsRequired = true)]
        public long ElapsedTimerTicks { get; set; }
        [DataMember(Name = "isValid", IsRequired = true)]
        public bool IsValid { get; set; }
    }
}
