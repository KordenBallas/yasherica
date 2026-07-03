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
