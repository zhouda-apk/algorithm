using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlgorithmLab.Data.FileStorage
{
    [DataContract]
    public sealed class TestCaseDto
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string Id { get; set; }
        [DataMember(Name = "name", IsRequired = true)]
        public string Name { get; set; }
        [DataMember(Name = "distribution", IsRequired = true)]
        public string Distribution { get; set; }
        [DataMember(Name = "generatorVersion", IsRequired = true)]
        public string GeneratorVersion { get; set; }
        [DataMember(Name = "createdAtUtc", IsRequired = true)]
        public string CreatedAtUtc { get; set; }
        [DataMember(Name = "updatedAtUtc", IsRequired = true)]
        public string UpdatedAtUtc { get; set; }
        [DataMember(Name = "revision", IsRequired = true)]
        public int Revision { get; set; }
        [DataMember(Name = "dataCount", IsRequired = true)]
        public int DataCount { get; set; }
        [DataMember(Name = "minValue", IsRequired = true)]
        public int MinValue { get; set; }
        [DataMember(Name = "maxValue", IsRequired = true)]
        public int MaxValue { get; set; }
        [DataMember(Name = "seed", IsRequired = true)]
        public int? Seed { get; set; }
        [DataMember(Name = "data", IsRequired = true)]
        public int[] Data { get; set; }
    }
}
