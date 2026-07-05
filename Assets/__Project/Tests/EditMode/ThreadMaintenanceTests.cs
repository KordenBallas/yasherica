using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ThreadMaintenanceTests
    {
        private FactStore _facts;
        private ThreadLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            // Permissive store (no registry): maintenance behavior under test, not vocabulary
            // validation — the shipped indicator key is declared in the demo registry asset.
            _facts = new FactStore();
            _ledger = new ThreadLedger();
        }

        private ThreadMaintenanceService Service(params ThreadDefinitionData[] definitions)
        {
            return new ThreadMaintenanceService(
                _ledger,
                new ThreadCatalog(definitions, defaultLifespanWindows: 3),
                new PreconditionEvaluator(new SubjectResolver(null), registry: null));
        }

        private static FactPredicate WorldEquals(string key, bool value)
        {
            return new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(value));
        }

        private static ThreadDefinitionData Definition(string id, ThreadKind kind,
            IReadOnlyList<FactPredicate> premise = null, IReadOnlyList<FactPredicate> resolution = null,
            int lifespan = 3)
        {
            return new ThreadDefinitionData(id, kind, premise, resolution, lifespan);
        }

        [Test]
        public void PremiseContradicted_FailsThread_Conflict()
        {
            // Premise: the rival order was NOT joined. B4 defaults make it hold until written.
            var service = Service(Definition("fox_arc", ThreadKind.Ephemeral,
                premise: new[] { WorldEquals("joined_lizards", false) }));
            _ledger.Open("fox_arc", ThreadKind.Ephemeral, 0);

            service.Tick(1, _facts);
            Assert.IsFalse(_ledger.IsRetired("fox_arc"));

            _facts.Set(new FactKey(FactNamespace.World, "", "joined_lizards"), FactValue.FromBool(true));
            service.Tick(2, _facts);

            Assert.IsTrue(_ledger.TryGet("fox_arc", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Conflict, record.Reason);
        }

        [Test]
        public void ArcThread_AlsoFailsOnConflict()
        {
            var service = Service(Definition("befriend_king", ThreadKind.Arc,
                premise: new[] { WorldEquals("king_dead", false) }));
            _ledger.Open("befriend_king", ThreadKind.Arc, 0);

            _facts.Set(new FactKey(FactNamespace.World, "", "king_dead"), FactValue.FromBool(true));
            service.Tick(1, _facts);

            Assert.IsTrue(_ledger.TryGet("befriend_king", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Conflict, record.Reason);
        }

        [Test]
        public void Retirement_WritesIndicatorFact_PerThreadSubject()
        {
            var service = Service(Definition("fox_arc", ThreadKind.Ephemeral,
                premise: new[] { WorldEquals("joined_lizards", false) }));
            _ledger.Open("fox_arc", ThreadKind.Ephemeral, 0);
            _facts.Set(new FactKey(FactNamespace.World, "", "joined_lizards"), FactValue.FromBool(true));

            service.Tick(1, _facts);

            var indicator = _facts.GetOrDefault(
                new FactKey(FactNamespace.World, "fox_arc", ThreadFactKeys.Retired),
                FactValue.FromString(""));
            Assert.AreEqual(ThreadFactKeys.RetiredValueConflict, indicator.AsString());
        }

        [Test]
        public void EphemeralThread_UnadvancedPastLifespan_Expires()
        {
            var service = Service(Definition("errand", ThreadKind.Ephemeral, lifespan: 2));
            _ledger.Open("errand", ThreadKind.Ephemeral, 0);

            service.Tick(1, _facts); // 1 window without advance
            service.Tick(2, _facts); // 2 - at the lifespan, still live
            Assert.IsFalse(_ledger.IsRetired("errand"));

            service.Tick(3, _facts); // 3 > lifespan - expires
            Assert.IsTrue(_ledger.TryGet("errand", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Expired, record.Reason);
            Assert.AreEqual(ThreadFactKeys.RetiredValueExpired, _facts.GetOrDefault(
                new FactKey(FactNamespace.World, "errand", ThreadFactKeys.Retired),
                FactValue.FromString("")).AsString());
        }

        [Test]
        public void ArcThread_UnadvancedPastLifespan_StaysLive()
        {
            var service = Service(Definition("spine", ThreadKind.Arc, lifespan: 1));
            _ledger.Open("spine", ThreadKind.Arc, 0);

            for (int window = 1; window <= 10; window++)
            {
                service.Tick(window, _facts);
            }

            Assert.IsFalse(_ledger.IsRetired("spine"));
        }

        [Test]
        public void AdvanceResetsExpiryCounter()
        {
            var service = Service(Definition("errand", ThreadKind.Ephemeral, lifespan: 2));
            _ledger.Open("errand", ThreadKind.Ephemeral, 0);

            service.Tick(1, _facts);
            service.Tick(2, _facts);
            _ledger.NoteBeatResolved("errand"); // player engaged between ticks
            service.Tick(3, _facts);            // fold: counter rearms to 0

            Assert.IsFalse(_ledger.IsRetired("errand"));
            Assert.IsTrue(_ledger.TryGet("errand", out var record));
            Assert.AreEqual(0, record.WindowsWithoutAdvance);

            service.Tick(4, _facts);
            service.Tick(5, _facts);
            Assert.IsFalse(_ledger.IsRetired("errand"));
            service.Tick(6, _facts);
            Assert.IsTrue(_ledger.IsRetired("errand"));
        }

        [Test]
        public void ConflictBeatsExpiry_WhenBothApply()
        {
            var service = Service(Definition("errand", ThreadKind.Ephemeral,
                premise: new[] { WorldEquals("door_open", true) }, lifespan: 1));
            _ledger.Open("errand", ThreadKind.Ephemeral, 0);
            // Premise already false (B4 default) and the clock will pass the lifespan on tick 2.
            service.Tick(1, _facts);

            Assert.IsTrue(_ledger.TryGet("errand", out var record));
            Assert.AreEqual(ThreadRetirementReason.Conflict, record.Reason);
        }

        [Test]
        public void ResolutionConditionsHold_ResolvesThread_NoIndicatorFact()
        {
            var service = Service(Definition("barn_raid", ThreadKind.Ephemeral,
                resolution: new[] { WorldEquals("grain_recovered", true) }));
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);

            _facts.Set(new FactKey(FactNamespace.World, "", "grain_recovered"), FactValue.FromBool(true));
            service.Tick(1, _facts);

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(ThreadState.Resolved, record.State);
            Assert.IsFalse(_facts.Has(new FactKey(FactNamespace.World, "barn_raid", ThreadFactKeys.Retired)));
        }

        [Test]
        public void ResolutionOutranksConflict_PayoffWins()
        {
            var service = Service(Definition("barn_raid", ThreadKind.Ephemeral,
                premise: new[] { WorldEquals("barn_intact", true) },
                resolution: new[] { WorldEquals("grain_recovered", true) }));
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _facts.Set(new FactKey(FactNamespace.World, "", "grain_recovered"), FactValue.FromBool(true));
            // Premise is false (B4 default for barn_intact=false vs required true) at the same tick.

            service.Tick(1, _facts);

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(ThreadState.Resolved, record.State);
        }

        [Test]
        public void UndeclaredThread_UsesImplicitEphemeralDefault_AndExpires()
        {
            var service = Service(); // empty catalog; default lifespan 3
            _ledger.Open("bare_label", ThreadKind.Ephemeral, 0);

            service.Tick(1, _facts);
            service.Tick(2, _facts);
            service.Tick(3, _facts);
            Assert.IsFalse(_ledger.IsRetired("bare_label"));
            service.Tick(4, _facts);
            Assert.IsTrue(_ledger.IsRetired("bare_label"));
        }

        [Test]
        public void Tick_RetiredThreads_AreSkipped_IndicatorNotRewritten()
        {
            var service = Service(Definition("errand", ThreadKind.Ephemeral, lifespan: 1));
            _ledger.Open("errand", ThreadKind.Ephemeral, 0);
            service.Tick(1, _facts);
            service.Tick(2, _facts);
            Assert.IsTrue(_ledger.IsRetired("errand"));

            // Overwrite the indicator; further ticks must not touch the retired thread again.
            _facts.Set(new FactKey(FactNamespace.World, "errand", ThreadFactKeys.Retired),
                FactValue.FromString("sentinel"));
            service.Tick(3, _facts);

            Assert.AreEqual("sentinel", _facts.GetOrDefault(
                new FactKey(FactNamespace.World, "errand", ThreadFactKeys.Retired),
                FactValue.FromString("")).AsString());
        }
    }
}
