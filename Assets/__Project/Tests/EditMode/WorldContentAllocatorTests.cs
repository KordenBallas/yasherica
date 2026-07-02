using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using Loot.Core;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class WorldContentAllocatorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeThemeProvider : ICurrentThemeProvider
        {
            private LevelTheme _theme = LevelTheme.Forest;
            public LevelTheme CurrentTheme => _theme;
            public void SetTheme(LevelTheme theme) => _theme = theme;
        }

        private static WorldContentDensitySettings Density(int avgPerQuest, int minSpacing,
            int empty, int loot, int combat) =>
            new WorldContentDensitySettings(avgPerQuest, minSpacing, empty, loot, combat);

        private static IBiomeMonsterPoolCatalog Pool(params int[] enemyIds) =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, enemyIds }
            });

        private static WorldContentAllocator Allocator(WorldContentDensitySettings density,
            IBiomeMonsterPoolCatalog pools = null, ulong seed = 7)
        {
            return new WorldContentAllocator(density,
                pools ?? new BiomeMonsterPoolCatalog(null),
                new FakeThemeProvider(),
                new DeterministicRandom(seed),
                new FakeLogger());
        }

        [Test]
        public void EmptyOnlyWeights_AllocateOnlyEmpty()
        {
            var allocator = Allocator(Density(avgPerQuest: 100, minSpacing: 0, empty: 1, loot: 0, combat: 0));

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(WorldSlotKind.Empty, allocator.AllocateSlot(questAvailable: false).Kind);
            }
        }

        [Test]
        public void CombatOnlyWeights_AllocateCombat_WithPoolEnemyIds()
        {
            var pool = Pool(5, 9);
            var allocator = Allocator(Density(avgPerQuest: 100, minSpacing: 0, empty: 0, loot: 0, combat: 1), pool);

            for (int i = 0; i < 10; i++)
            {
                var slot = allocator.AllocateSlot(questAvailable: false);
                Assert.AreEqual(WorldSlotKind.Combat, slot.Kind);
                Assert.Contains(slot.EnemyId, new[] { 5, 9 });
            }
        }

        [Test]
        public void QuestEverySlot_WhenAvailableAndNoSpacing()
        {
            var allocator = Allocator(Density(avgPerQuest: 1, minSpacing: 0, empty: 1, loot: 0, combat: 0));

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(WorldSlotKind.Quest, allocator.AllocateSlot(questAvailable: true).Kind);
            }
        }

        [Test]
        public void MinSpacing_IsAHardInvariant()
        {
            const int spacing = 2;
            var allocator = Allocator(Density(avgPerQuest: 1, minSpacing: spacing, empty: 1, loot: 0, combat: 0));

            int lastQuestIndex = -(spacing + 1); // window-0 head start: a quest may land immediately
            for (int i = 0; i < 30; i++)
            {
                if (allocator.AllocateSlot(questAvailable: true).Kind == WorldSlotKind.Quest)
                {
                    Assert.Greater(i - lastQuestIndex, spacing,
                        $"Quests at slots {lastQuestIndex} and {i} violate the min spacing of {spacing}.");
                    lastQuestIndex = i;
                }
            }

            Assert.GreaterOrEqual(lastQuestIndex, 0, "Expected at least one quest in 30 slots.");
        }

        [Test]
        public void QuestUnavailable_FallsBackToAmbient_WithoutResettingSpacing()
        {
            const int spacing = 2;
            var allocator = Allocator(Density(avgPerQuest: 1, minSpacing: spacing, empty: 1, loot: 0, combat: 0));

            Assert.AreEqual(WorldSlotKind.Quest, allocator.AllocateSlot(questAvailable: true).Kind);

            // Two unavailable slots degrade to ambient; the counter keeps running underneath.
            Assert.AreEqual(WorldSlotKind.Empty, allocator.AllocateSlot(questAvailable: false).Kind);
            Assert.AreEqual(WorldSlotKind.Empty, allocator.AllocateSlot(questAvailable: false).Kind);

            // Spacing (2) has been served by the two ambient slots, so the quest lands immediately.
            Assert.AreEqual(WorldSlotKind.Quest, allocator.AllocateSlot(questAvailable: true).Kind);
        }

        [Test]
        public void EmptyBiomePool_DowngradesCombatToEmpty()
        {
            var allocator = Allocator(Density(avgPerQuest: 100, minSpacing: 0, empty: 0, loot: 0, combat: 1));

            Assert.AreEqual(WorldSlotKind.Empty, allocator.AllocateSlot(questAvailable: false).Kind);
        }

        [Test]
        public void AllZeroWeights_AllocateEmpty()
        {
            var allocator = Allocator(Density(avgPerQuest: 100, minSpacing: 0, empty: 0, loot: 0, combat: 0));

            Assert.AreEqual(WorldSlotKind.Empty, allocator.AllocateSlot(questAvailable: false).Kind);
        }

        [Test]
        public void SameSeed_ProducesIdenticalAllocationSequence()
        {
            var density = Density(avgPerQuest: 3, minSpacing: 1, empty: 2, loot: 1, combat: 1);

            var a = Allocator(density, Pool(5, 9), seed: 42);
            var b = Allocator(density, Pool(5, 9), seed: 42);
            for (int i = 0; i < 40; i++)
            {
                var slotA = a.AllocateSlot(questAvailable: true);
                var slotB = b.AllocateSlot(questAvailable: true);
                Assert.AreEqual(slotA.Kind, slotB.Kind, $"Kind diverged at slot {i}.");
                Assert.AreEqual(slotA.EnemyId, slotB.EnemyId, $"EnemyId diverged at slot {i}.");
            }
        }

        [Test]
        public void MixedWeights_ProduceEachAmbientKind()
        {
            var allocator = Allocator(Density(avgPerQuest: 100, minSpacing: 0, empty: 1, loot: 1, combat: 1), Pool(5));

            var seen = new HashSet<WorldSlotKind>();
            for (int i = 0; i < 60; i++)
            {
                seen.Add(allocator.AllocateSlot(questAvailable: false).Kind);
            }

            CollectionAssert.IsSubsetOf(new[] { WorldSlotKind.Empty, WorldSlotKind.Loot, WorldSlotKind.Combat }, seen);
        }
    }
}
