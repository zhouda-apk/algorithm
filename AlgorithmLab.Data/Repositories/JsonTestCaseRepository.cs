using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Data.FileStorage;
namespace AlgorithmLab.Data.Repositories
{
    public sealed class JsonTestCaseRepository : ITestCaseRepository
    {
        private readonly JsonDataStore store;
        public JsonTestCaseRepository(JsonDataStore store) { this.store = store ?? throw new ArgumentNullException(nameof(store)); }
        private static TestCase FromDto(TestCaseDto dto)
        {
            var draft = new TestCaseDraft(dto.Name, (DataDistribution)Enum.Parse(typeof(DataDistribution), dto.Distribution), dto.Data, dto.Seed, dto.GeneratorVersion, dto.MinValue, dto.MaxValue);
            return new TestCase(Guid.Parse(dto.Id), dto.Revision, draft, DateTime.ParseExact(dto.CreatedAtUtc, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTime.ParseExact(dto.UpdatedAtUtc, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }
        private static TestCaseDto ToDto(TestCase value)
        {
            return new TestCaseDto { Id = value.Id.ToString("D"), Name = value.Name, Revision = value.Revision, Distribution = value.Distribution.ToString(), DataCount = value.DataCount,
                Seed = value.Seed, GeneratorVersion = value.GeneratorVersion, MinValue = value.MinValue, MaxValue = value.MaxValue, Data = value.Data,
                CreatedAtUtc = value.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture), UpdatedAtUtc = value.UpdatedAtUtc.ToString("o", CultureInfo.InvariantCulture) };
        }
        private static TestCaseDto Find(StoreDocument document, Guid id)
        {
            var c = document.TestCases.SingleOrDefault(v => Guid.Parse(v.Id) == id);
            if (c == null) throw new AlgorithmLabException(ErrorKind.NotFound, "找不到案例。");
            return c;
        }
        public IReadOnlyList<TestCaseSummary> GetSummaries()
        { return store.Load().TestCases.Select(c => new TestCaseSummary(FromDto(c))).OrderByDescending(c => c.UpdatedAtUtc).ThenBy(c => c.Id).ToList().AsReadOnly(); }
        public TestCase GetById(Guid id) { return FromDto(Find(store.Load(), id)); }
        public void Add(TestCase value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Revision != 1) throw new AlgorithmLabException(ErrorKind.Validation, "新案例版次必須為 1。");
            store.Commit(d => { if (d.TestCases.Any(c => Guid.Parse(c.Id) == value.Id)) throw new AlgorithmLabException(ErrorKind.Conflict, "案例 ID 已存在。"); d.TestCases.Add(ToDto(value)); });
        }
        public void Update(TestCase replacement, int expectedRevision)
        {
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            store.Commit(d => {
                var old = Find(d, replacement.Id);
                if (old.Revision != expectedRevision) throw new AlgorithmLabException(ErrorKind.Conflict, "案例版次已改變。");
                if ((long)replacement.Revision != (long)expectedRevision + 1 || replacement.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture) != old.CreatedAtUtc || replacement.UpdatedAtUtc < DateTime.ParseExact(old.UpdatedAtUtc, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                    throw new AlgorithmLabException(ErrorKind.Validation, "更新版次或時間不符。");
                d.TestCases[d.TestCases.IndexOf(old)] = ToDto(replacement);
            });
        }
        public void Delete(Guid id, int expectedRevision)
        {
            store.Commit(d => {
                var old = Find(d, id);
                if (old.Revision != expectedRevision) throw new AlgorithmLabException(ErrorKind.Conflict, "案例版次已改變。");
                d.TestCases.Remove(old);
                foreach (var run in d.BenchmarkRuns.Where(r => r.TestCaseId != null && Guid.Parse(r.TestCaseId) == id)) run.TestCaseId = null;
            });
        }
    }
}
