using Core.Persistence;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The run counter (D7/P3-1 soft-floor input): <c>world.run_count</c> advances exactly once per
    /// fresh run start and never on a continue — a continue is the same run resuming.
    /// </summary>
    [TestFixture]
    public class RunCounterServiceTests
    {
        private FactStore _store;

        [SetUp]
        public void SetUp()
        {
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "run_count", FactScope.Global, FactValueType.Int,
                    FactValue.FromInt(0))
            });
            _store = new FactStore(registry);
        }

        [Test]
        public void FreshBoot_IncrementsFromTheLoadedMemory()
        {
            // The meta bootstrap already restored the persisted counter; the first run of a fresh
            // profile starts from the registry default 0 and becomes run 1.
            new RunCounterService(new RunRestoreContext(null), _store).Initialize();
            Assert.AreEqual(1, _store.GetInt(WorldFacts.RunCount));

            // A later profile boot with two prior runs remembered becomes run 3.
            _store.SetInt(WorldFacts.RunCount, 2);
            new RunCounterService(new RunRestoreContext(null), _store).Initialize();
            Assert.AreEqual(3, _store.GetInt(WorldFacts.RunCount));
        }

        [Test]
        public void ContinueBoot_DoesNotCountANewRun()
        {
            _store.SetInt(WorldFacts.RunCount, 2);
            new RunCounterService(new RunRestoreContext(new RunSaveSnapshot()), _store).Initialize();
            Assert.AreEqual(2, _store.GetInt(WorldFacts.RunCount));
        }
    }
}
