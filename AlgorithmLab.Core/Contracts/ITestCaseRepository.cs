using System;
using System.Collections.Generic;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Contracts
{
    public interface ITestCaseRepository
    {
        IReadOnlyList<TestCaseSummary> GetSummaries();
        TestCase GetById(Guid id);
        void Add(TestCase testCase);
        void Update(TestCase replacement, int expectedRevision);
        void Delete(Guid id, int expectedRevision);
    }
}
