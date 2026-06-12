using System;
using Loot.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LootSeedTests
    {
        [Test]
        public void Derive_SameInputs_ReturnsSameSeed()
        {
            var first = LootSeed.Derive(12345, "quest:story_a:npc_b");
            var second = LootSeed.Derive(12345, "quest:story_a:npc_b");

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Derive_DifferentContextKeys_ReturnDifferentSeeds()
        {
            var first = LootSeed.Derive(12345, "platform:1");
            var second = LootSeed.Derive(12345, "platform:2");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Derive_DifferentRunSeeds_ReturnDifferentSeeds()
        {
            var first = LootSeed.Derive(1, "platform:1");
            var second = LootSeed.Derive(2, "platform:1");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Derive_NullOrEmptyKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => LootSeed.Derive(1, null));
            Assert.Throws<ArgumentException>(() => LootSeed.Derive(1, string.Empty));
        }
    }
}
