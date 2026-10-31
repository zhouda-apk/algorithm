using System;
using System.Collections.Generic;
using System.Linq;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public sealed class TestCaseService
    {
        private readonly ITestCaseRepository repository;
        public TestCaseService(ITestCaseRepository repository) { this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); }
        public TestCase Create(TestCaseDraft draft)
        {
            var now = DateTime.UtcNow; var value = new TestCase(Guid.NewGuid(), 1, draft, now, now);
            repository.Add(value); return value;
        }
        public IReadOnlyList<TestCaseSummary> Search(string keyword = null, DataDistribution? distribution = null)
        {
            CaseRules.Require(!distribution.HasValue || Enum.IsDefined(typeof(DataDistribution), distribution.Value), "未知分布。");
            keyword = (keyword ?? "").Trim();
            return repository.GetSummaries().Where(c => c.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 && (!distribution.HasValue || c.Distribution == distribution.Value))
                .OrderByDescending(c => c.UpdatedAtUtc).ThenBy(c => c.Id).ToList().AsReadOnly();
        }
        public TestCase GetDetails(Guid id) { return repository.GetById(id); }
        public TestCase Update(Guid id, int expectedRevision, TestCaseDraft draft)
        {
            var old = repository.GetById(id);
            if (old.Revision != expectedRevision) throw new AlgorithmLabException(ErrorKind.Conflict, "案例版次已改變，請重新載入。");
            CaseRules.Require(old.Revision < int.MaxValue, "案例版次已達上限。");
            var now = DateTime.UtcNow; if (now < old.UpdatedAtUtc) now = old.UpdatedAtUtc;
            var next = new TestCase(id, old.Revision + 1, draft, old.CreatedAtUtc, now);
            repository.Update(next, expectedRevision); return next;
        }
        public void Delete(Guid id, int expectedRevision) { repository.Delete(id, expectedRevision); }
        public TestCaseSnapshot CreateSnapshot(Guid id, int expectedRevision)
        {
            var value = repository.GetById(id);
            if (value.Revision != expectedRevision) throw new AlgorithmLabException(ErrorKind.Conflict, "案例版次已改變，請重新選取。");
            return new TestCaseSnapshot(value);
        }
    }
}
