using System;
using System.Collections.Generic;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public sealed class AlgorithmCatalog
    {
        private readonly Dictionary<string, Func<ISortAlgorithm>> factories = new Dictionary<string, Func<ISortAlgorithm>>(StringComparer.Ordinal);
        private readonly List<AlgorithmDescriptor> descriptors = new List<AlgorithmDescriptor>();
        public AlgorithmCatalog(IEnumerable<Func<ISortAlgorithm>> registrations)
        {
            if (registrations == null) throw new ArgumentNullException(nameof(registrations));
            foreach (var factory in registrations)
            {
                if (factory == null) throw new AlgorithmLabException(ErrorKind.Validation, "Algorithm factory is null.");
                var algorithm = factory();
                if (algorithm == null || algorithm.Descriptor == null) throw new AlgorithmLabException(ErrorKind.Validation, "Algorithm descriptor is missing.");
                var descriptor = algorithm.Descriptor;
                if (factories.ContainsKey(descriptor.Id)) throw new AlgorithmLabException(ErrorKind.Validation, "Duplicate algorithm Id: " + descriptor.Id);
                factories.Add(descriptor.Id, factory); descriptors.Add(descriptor);
            }
        }
        public IReadOnlyList<AlgorithmDescriptor> GetAll() { return descriptors.AsReadOnly(); }
        public ISortAlgorithm GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new AlgorithmLabException(ErrorKind.Validation, "Algorithm Id is required.");
            Func<ISortAlgorithm> factory;
            if (!factories.TryGetValue(id, out factory)) throw new AlgorithmLabException(ErrorKind.NotFound, "Unknown algorithm Id: " + id);
            var value = factory(); var descriptor = descriptors.Find(d => d.Id == id);
            if (value == null || value.Descriptor == null || value.Descriptor.Id != id || value.Descriptor.Name != descriptor.Name || value.Descriptor.Version != descriptor.Version || value.Descriptor.TimeComplexityDescription != descriptor.TimeComplexityDescription || value.Descriptor.IsStable != descriptor.IsStable || value.Descriptor.IsMidtermAlgorithm != descriptor.IsMidtermAlgorithm)
                throw new AlgorithmLabException(ErrorKind.Validation, "Algorithm factory changed its registered descriptor.");
            return value;
        }
    }
}
