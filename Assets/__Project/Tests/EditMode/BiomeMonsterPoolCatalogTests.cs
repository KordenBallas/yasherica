using System.Collections.Generic;
using LevelGeneration;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeMonsterPoolCatalogTests
    {
        private static BiomeMonsterPoolCatalog Tagged() =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>
            {
                {
                    LevelTheme.Forest, new[]
                    {
                        new MonsterPoolEntry(1, new[] { "wild-beast" }),
                        new MonsterPoolEntry(2, new[] { "bandit", "humanoid" }),
                        new MonsterPoolEntry(3, new[] { "Bandit" })
                    }
                }
            });

        // 1 belongs to the backwater (tier 1 only); 2 opens at tier 2+; 3 is unbanded (every tier).
        private static BiomeMonsterPoolCatalog Banded() =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>
            {
                {
                    LevelTheme.Forest, new[]
                    {
                        new MonsterPoolEntry(1, new[] { "wild-beast" }, new RunTierBand(1, 1)),
                        new MonsterPoolEntry(2, new[] { "bandit" }, new RunTierBand(2, 0)),
                        new MonsterPoolEntry(3, new[] { "bandit" })
                    }
                }
            });

        [Test]
        public void TierLookup_FiltersByBand_AndShiftsWithTheClimb()
        {
            CollectionAssert.AreEqual(new[] { 1, 3 }, Banded().GetPool(LevelTheme.Forest, 1));
            CollectionAssert.AreEqual(new[] { 2, 3 }, Banded().GetPool(LevelTheme.Forest, 2));
            // The backwater creature (1) has aged out by tier 3; 2 stays open, 3 is unbanded.
            CollectionAssert.AreEqual(new[] { 2, 3 }, Banded().GetPool(LevelTheme.Forest, 3));
        }

        [Test]
        public void UnbandedEntry_IsEligibleAtEveryTier()
        {
            CollectionAssert.Contains(Banded().GetPool(LevelTheme.Forest, 99), 3);
        }

        [Test]
        public void FlavorAndTier_Compose()
        {
            // "bandit" carries 2 and 3; at tier 1 only 3 is in band, at tier 2 both are.
            CollectionAssert.AreEqual(new[] { 3 }, Banded().GetPool(LevelTheme.Forest, "bandit", 1));
            CollectionAssert.AreEqual(new[] { 2, 3 }, Banded().GetPool(LevelTheme.Forest, "bandit", 2));
        }

        [Test]
        public void EmptyFlavorWithTier_FallsThroughToTheTierFilteredPool()
        {
            CollectionAssert.AreEqual(new[] { 2, 3 }, Banded().GetPool(LevelTheme.Forest, null, 2));
        }

        [Test]
        public void TierLookup_UnknownTheme_ReturnsEmpty()
        {
            Assert.AreEqual(0, Banded().GetPool(LevelTheme.Desert, 1).Count);
            Assert.AreEqual(0, Banded().GetPool(LevelTheme.Desert, "bandit", 1).Count);
        }

        [Test]
        public void UnflavoredLookup_ReturnsTheWholePool()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Tagged().GetPool(LevelTheme.Forest));
        }

        [Test]
        public void FlavoredLookup_FiltersByTag_CaseInsensitively()
        {
            CollectionAssert.AreEqual(new[] { 2, 3 }, Tagged().GetPool(LevelTheme.Forest, "bandit"));
        }

        [Test]
        public void EmptyFlavor_FallsThroughToTheUnfilteredPool()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Tagged().GetPool(LevelTheme.Forest, null));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Tagged().GetPool(LevelTheme.Forest, string.Empty));
        }

        [Test]
        public void UnmatchedFlavor_ReturnsEmpty_TheCallerOwnsTheFallback()
        {
            Assert.AreEqual(0, Tagged().GetPool(LevelTheme.Forest, "guard").Count);
        }

        [Test]
        public void UnknownTheme_ReturnsEmpty()
        {
            Assert.AreEqual(0, Tagged().GetPool(LevelTheme.Desert).Count);
            Assert.AreEqual(0, Tagged().GetPool(LevelTheme.Desert, "bandit").Count);
        }

        [Test]
        public void IdsOnlyConstructor_BehavesUntagged()
        {
            var catalog = new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, new[] { 7, 9 } }
            });

            CollectionAssert.AreEqual(new[] { 7, 9 }, catalog.GetPool(LevelTheme.Forest));
            Assert.AreEqual(0, catalog.GetPool(LevelTheme.Forest, "bandit").Count);
        }
    }
}
