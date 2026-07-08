using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MetaVocabularyTests
    {
        private static FactStoreSnapshot Snapshot(params FactEntryDto[] entries)
        {
            var snapshot = new FactStoreSnapshot();
            snapshot.Entries.AddRange(entries);
            return snapshot;
        }

        private static FactEntryDto TastedEntry(string partId) => new FactEntryDto
        {
            Namespace = FactNamespace.World,
            Subject = partId,
            Key = "arena_tasted",
            Type = FactValueType.Bool,
            BoolValue = true
        };

        private static MetaGate TasteGate(int tier = 0) => new MetaGate(
            GatingMark.MetaGated,
            new[]
            {
                new FactPredicate(FactNamespace.World, "$self", "arena_tasted", ComparisonOp.Eq,
                    FactValue.FromBool(true))
            },
            tier,
            1f);

        private static MetaProgressionSettings SettingsWithFloors(params int[] floors) =>
            new MetaProgressionSettings(floors, 4, 1f, 1f, 1f, 0.75f, 3, false, 1f, 8);

        [Test]
        public void BaseOrNullGate_AlwaysUnlocked()
        {
            var vocabulary = new MetaVocabulary(null, MetaProgressionSettings.Defaults, 1);

            Assert.IsTrue(vocabulary.IsUnlocked("anything", null));
            Assert.IsTrue(vocabulary.IsUnlocked("anything", MetaGate.Base));
        }

        [Test]
        public void GatedToken_LockedUntilDeedMet()
        {
            var settings = SettingsWithFloors(1);
            var locked = new MetaVocabulary(Snapshot(), settings, 1);
            var unlocked = new MetaVocabulary(Snapshot(TastedEntry("part_serpent")), settings, 1);

            Assert.IsFalse(locked.IsUnlocked("part_serpent", TasteGate()));
            Assert.IsTrue(unlocked.IsUnlocked("part_serpent", TasteGate()));
        }

        [Test]
        public void TierRunFloor_HoldsThenReleases()
        {
            var settings = SettingsWithFloors(1, 3);
            var snapshot = Snapshot(TastedEntry("part_serpent"));

            Assert.IsFalse(new MetaVocabulary(snapshot, settings, 2).IsUnlocked("part_serpent", TasteGate(tier: 1)));
            Assert.IsTrue(new MetaVocabulary(snapshot, settings, 3).IsUnlocked("part_serpent", TasteGate(tier: 1)));
        }

        [Test]
        public void TierBeyondAuthoredFloors_SharesLastFloor()
        {
            var settings = SettingsWithFloors(1, 3);
            var snapshot = Snapshot(TastedEntry("part_serpent"));

            Assert.IsFalse(new MetaVocabulary(snapshot, settings, 2).IsUnlocked("part_serpent", TasteGate(tier: 9)));
            Assert.IsTrue(new MetaVocabulary(snapshot, settings, 3).IsUnlocked("part_serpent", TasteGate(tier: 9)));
        }

        [Test]
        public void GatedTokenWithEmptyDeed_GatedByFloorAlone()
        {
            var settings = SettingsWithFloors(1, 3);
            var gate = new MetaGate(GatingMark.MetaGated, null, 1, 1f);

            Assert.IsFalse(new MetaVocabulary(Snapshot(), settings, 2).IsUnlocked("token", gate));
            Assert.IsTrue(new MetaVocabulary(Snapshot(), settings, 3).IsUnlocked("token", gate));
        }

        [Test]
        public void EffectiveRunCount_OverridesSnapshotRunCount()
        {
            // The snapshot claims run 9, but this vocabulary serves run 1: run-counter deeds must
            // read the run being served, not the stale stored value.
            var snapshot = Snapshot(new FactEntryDto
            {
                Namespace = FactNamespace.World,
                Subject = string.Empty,
                Key = "run_count",
                Type = FactValueType.Int,
                IntValue = 9
            });
            var gate = new MetaGate(
                GatingMark.MetaGated,
                new[]
                {
                    new FactPredicate(FactNamespace.World, string.Empty, "run_count", ComparisonOp.Gte,
                        FactValue.FromInt(3))
                },
                0,
                1f);

            Assert.IsFalse(new MetaVocabulary(snapshot, SettingsWithFloors(0), 1).IsUnlocked("token", gate));
            Assert.IsTrue(new MetaVocabulary(snapshot, SettingsWithFloors(0), 3).IsUnlocked("token", gate));
        }

        [Test]
        public void NullOrEmptySnapshot_DegradesToBaseOnly()
        {
            foreach (var snapshot in new[] { null, Snapshot() })
            {
                var vocabulary = new MetaVocabulary(snapshot, MetaProgressionSettings.Defaults, 1);

                Assert.IsTrue(vocabulary.IsUnlocked("token", MetaGate.Base));
                Assert.IsFalse(vocabulary.IsUnlocked("token", TasteGate()));
            }
        }

        [Test]
        public void NullSettings_FallBackToDefaults()
        {
            var vocabulary = new MetaVocabulary(Snapshot(TastedEntry("p")), null, 1);

            Assert.IsTrue(vocabulary.IsUnlocked("p", TasteGate()));
        }

        [Test]
        public void SameSnapshotAndSettings_GiveIdenticalAnswers()
        {
            var snapshot = Snapshot(TastedEntry("part_a"), TastedEntry("part_b"));
            var settings = SettingsWithFloors(1, 2, 5);
            var first = new MetaVocabulary(snapshot, settings, 4);
            var second = new MetaVocabulary(snapshot, settings, 4);

            foreach (var token in new[] { "part_a", "part_b", "part_c" })
            {
                foreach (var tier in new[] { 0, 1, 2 })
                {
                    Assert.AreEqual(
                        first.IsUnlocked(token, TasteGate(tier)),
                        second.IsUnlocked(token, TasteGate(tier)),
                        $"token={token} tier={tier}");
                }
            }
        }
    }
}
