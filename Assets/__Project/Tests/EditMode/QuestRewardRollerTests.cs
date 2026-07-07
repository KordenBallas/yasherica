using System;
using Loot.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class QuestRewardRollerTests
    {
        private sealed class SeedProvider : IRunSeedProvider
        {
            private int _seed;
            public SeedProvider(int seed) { _seed = seed; }
            public int RunSeed => _seed;
            public void SetSeed(int seed) => _seed = seed;
        }

        private static QuestRewardPools Pools() => new QuestRewardPools(
            new[]
            {
                new RewardArtifactOption("iron_fang", 1, "power"),
                new RewardArtifactOption("ember_core", 1, "power"),
                new RewardArtifactOption("war_maul", 2, "power"),
                new RewardArtifactOption("rope", 0, "utility"),
                new RewardArtifactOption("lantern", 1, "utility")
            },
            new[]
            {
                new RewardBlankOption("blank.fox_leg", "fox"),
                new RewardBlankOption("blank.fox_ear", "fox"),
                new RewardBlankOption("blank.ibex_horn", "ibex"),
                new RewardBlankOption("blank.bare", "")
            });

        private static QuestRewardRoller Roller(int seed = 7) =>
            new QuestRewardRoller(Pools(), new SeedProvider(seed));

        [Test]
        public void ArtifactRoll_HonoursDeclaredFamily()
        {
            var reward = new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact);

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(Roller().TryRoll(reward, $"q:{i}", out var result));
                CollectionAssert.Contains(new[] { "iron_fang", "ember_core" }, result.DefinitionId,
                    "a family-constrained roll must stay in family AND at the declared tier");
            }
        }

        [Test]
        public void ArtifactRoll_PrefersExactTier_DegradesToNearest()
        {
            // Tier 3 does not exist in the power family; tier-2 war_maul is the nearest.
            var reward = new QuestRewardCore(3, "power", QuestRewardPayloadKind.Artifact);

            Assert.IsTrue(Roller().TryRoll(reward, "q:0", out var result));
            Assert.AreEqual("war_maul", result.DefinitionId);
        }

        [Test]
        public void ArtifactRoll_UnknownFamily_FailsInsteadOfSubstituting()
        {
            var reward = new QuestRewardCore(1, "does_not_exist", QuestRewardPayloadKind.Artifact);

            Assert.IsFalse(Roller().TryRoll(reward, "q:0", out _),
                "the colour promise is hard - never pay a family the card did not show");
        }

        [Test]
        public void BlankRoll_HonoursDeclaredRace()
        {
            var reward = new QuestRewardCore(1, "fox", QuestRewardPayloadKind.PartBlank);

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(Roller().TryRoll(reward, $"q:{i}", out var result));
                Assert.AreEqual(QuestRewardPayloadKind.PartBlank, result.PayloadKind);
                StringAssert.StartsWith("blank.fox_", result.DefinitionId);
            }
        }

        [Test]
        public void BlankRoll_EmptyBelonging_DrawsFromWholePool()
        {
            var reward = new QuestRewardCore(0, "", QuestRewardPayloadKind.PartBlank);
            bool sawNonFox = false;
            for (int i = 0; i < 40 && !sawNonFox; i++)
            {
                Roller().TryRoll(reward, $"q:{i}", out var result);
                sawNonFox = !result.DefinitionId.StartsWith("blank.fox_", StringComparison.Ordinal);
            }

            Assert.IsTrue(sawNonFox, "an unconstrained declaration draws beyond one race");
        }

        [Test]
        public void SameSeedAndContext_RollsTheSameItem()
        {
            var reward = new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact);

            Assert.IsTrue(Roller(99).TryRoll(reward, "qst_x:0", out var first));
            Assert.IsTrue(Roller(99).TryRoll(reward, "qst_x:0", out var second));

            Assert.AreEqual(first.DefinitionId, second.DefinitionId, "deterministic under the run seed");
        }

        [Test]
        public void DifferentSeeds_CanRollDifferentItems()
        {
            var reward = new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact);
            bool diverged = false;
            for (int seed = 0; seed < 30 && !diverged; seed++)
            {
                Roller(seed).TryRoll(reward, "qst_x:0", out var a);
                Roller(seed + 1000).TryRoll(reward, "qst_x:0", out var b);
                diverged = a.DefinitionId != b.DefinitionId;
            }

            Assert.IsTrue(diverged, "the reward is a roll, not a fixed catalog item (non-catalog world)");
        }
    }
}
