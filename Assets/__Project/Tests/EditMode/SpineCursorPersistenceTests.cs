using System;
using System.IO;
using Core.Persistence;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The spine cursor across the meta file boundary (D20, P3-3): a seen flag written in run 1 is
    /// flushed with the Meta partition (the defeat path flushes before consuming the run save) and
    /// restored into run 2's store with its per-story subject intact; a corrupt memory quarantines
    /// and the cursor degrades to empty - the run still runs. Real file IO against a temp dir.
    /// </summary>
    [TestFixture]
    public class SpineCursorPersistenceTests
    {
        private string _directory;
        private UnityJsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "yasherica-cursor-tests-" + Guid.NewGuid().ToString("N"));
            _serializer = new UnityJsonSaveSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static FactKeyRegistry Registry() => new FactKeyRegistry(new[]
        {
            new FactKeyInfo(FactNamespace.World, "spine_seen", FactScope.PerStory, FactValueType.Bool,
                FactValue.FromBool(false), FactHorizon.Meta),
            new FactKeyInfo(FactNamespace.World, "barn_raided", FactScope.Global, FactValueType.Bool,
                FactValue.FromBool(false))
        });

        [Test]
        public void SeenFact_FlushedOnDefeat_SurvivesIntoRunTwo_WithItsStorySubject()
        {
            var registry = Registry();
            var metaStore = new MetaMemoryStore(_directory, _serializer);

            // Run 1: the recorder wrote the seen flag mid-run; a run-scoped fact sits beside it.
            var runOneFacts = new FactStore();
            runOneFacts.SetBool(WorldFacts.SpineSeen, true, "story_spine_cauldron_hint");
            runOneFacts.Set(new FactKey(FactNamespace.World, "", "barn_raided"), FactValue.FromBool(true));
            // Defeat: meta is flushed, then the run save is consumed - the losing run's reveal sticks.
            new MetaMemoryFlushService(metaStore, runOneFacts, registry).Flush();

            // Run 2: a fresh store; the bootstrap restores the world memory.
            var runTwoFacts = new FactStore();
            new MetaMemoryBootstrap(new MetaMemoryStore(_directory, _serializer), runTwoFacts).Initialize();

            Assert.IsTrue(runTwoFacts.GetBool(WorldFacts.SpineSeen, "story_spine_cauldron_hint"),
                "the seen flag must survive death with its per-story subject intact");
            Assert.IsFalse(runTwoFacts.GetBool(WorldFacts.SpineSeen, "story_spine_mirror_glimpse"),
                "other beats stay unseen");
            Assert.IsFalse(runTwoFacts.Has(new FactKey(FactNamespace.World, "", "barn_raided")),
                "run-scoped facts must not ride the cursor's file");
        }

        [Test]
        public void CorruptMetaFile_Quarantines_AndTheCursorDegradesToEmpty()
        {
            Directory.CreateDirectory(_directory);
            var metaPath = Path.Combine(_directory, MetaMemoryStore.FileName);
            File.WriteAllText(metaPath, "{ not a snapshot");

            var facts = new FactStore();
            new MetaMemoryBootstrap(new MetaMemoryStore(_directory, _serializer), facts).Initialize();

            Assert.IsFalse(facts.GetBool(WorldFacts.SpineSeen, "story_spine_cauldron_hint"),
                "no memory means no cursor - every beat is fresh, nothing crashes");
            Assert.AreEqual(0, facts.Snapshot().Count);
        }
    }
}
