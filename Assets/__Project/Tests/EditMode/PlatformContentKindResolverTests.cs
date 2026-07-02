using System.Collections.Generic;
using LevelGeneration;
using LevelGeneration.Surface;
using NUnit.Framework;
using Platform;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformContentKindResolverTests
    {
        private static GraphNode Node(PlatformType type, params PlatformContentType[] contentTypes)
        {
            return new GraphNode
            {
                Id = 1,
                Type = type,
                ContentTypes = new List<PlatformContentType>(contentTypes)
            };
        }

        [Test]
        public void CombatType_WinsRegardlessOfContent()
        {
            // The streaming coordinator sets Type=Combat from PlannedPlatform.IsCombat, which covers
            // both ambient monsters and story NPCs with a required combat slot.
            Assert.AreEqual(PlatformContentKind.Combat,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Combat, PlatformContentType.Enemy)));
            Assert.AreEqual(PlatformContentKind.Combat,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Combat, PlatformContentType.Npc)));
            Assert.AreEqual(PlatformContentKind.Combat,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Combat)));
        }

        [Test]
        public void NpcContent_OnSimplePlatform_ResolvesNpc()
        {
            Assert.AreEqual(PlatformContentKind.Npc,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Simple, PlatformContentType.Npc)));
        }

        [Test]
        public void EnemyContent_OnSimplePlatform_StillResolvesCombat()
        {
            // Defensive: the legacy one-shot path may mark enemies without the Combat node type.
            Assert.AreEqual(PlatformContentKind.Combat,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Simple, PlatformContentType.Enemy)));
        }

        [Test]
        public void LootContent_ResolvesLoot()
        {
            Assert.AreEqual(PlatformContentKind.Loot,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Simple, PlatformContentType.Loot)));
        }

        [Test]
        public void NoneOrEmptyContent_ResolvesEmpty()
        {
            Assert.AreEqual(PlatformContentKind.Empty,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Simple, PlatformContentType.None)));
            Assert.AreEqual(PlatformContentKind.Empty,
                PlatformContentKindResolver.Resolve(Node(PlatformType.Simple)));
            Assert.AreEqual(PlatformContentKind.Empty,
                PlatformContentKindResolver.Resolve(new GraphNode { Id = 2, Type = PlatformType.Simple, ContentTypes = null }));
            Assert.AreEqual(PlatformContentKind.Empty, PlatformContentKindResolver.Resolve(null));
        }

        [Test]
        public void NpcBeatsLoot_WhenBothPresent()
        {
            Assert.AreEqual(PlatformContentKind.Npc,
                PlatformContentKindResolver.Resolve(
                    Node(PlatformType.Simple, PlatformContentType.Loot, PlatformContentType.Npc)));
        }
    }
}
