using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ActorInstanceFactoryTests
    {
        private static NpcArchetypeData Archetype(params string[] namePool) =>
            new NpcArchetypeData("arch_road_bandit", namePool, "free_blades", 0, new[] { "bandit" });

        [Test]
        public void Create_PropagatesArchetypeAndFaction()
        {
            var factory = new ActorInstanceFactory(new DeterministicRandom(1));

            var instance = factory.Create(Archetype("Razor", "Mauler"));

            Assert.AreEqual("arch_road_bandit", instance.ArchetypeId);
            Assert.AreEqual("free_blades", instance.FactionId);
        }

        [Test]
        public void Create_PicksDisplayNameFromPool()
        {
            var pool = new[] { "Razor", "Mauler", "Grin" };
            var factory = new ActorInstanceFactory(new DeterministicRandom(1));

            var instance = factory.Create(Archetype(pool));

            CollectionAssert.Contains(pool, instance.ChosenDisplayName);
        }

        [Test]
        public void Create_NamePick_IsDeterministicUnderSameSeed()
        {
            var a = new ActorInstanceFactory(new DeterministicRandom(42)).Create(Archetype("Razor", "Mauler", "Grin"));
            var b = new ActorInstanceFactory(new DeterministicRandom(42)).Create(Archetype("Razor", "Mauler", "Grin"));

            Assert.AreEqual(a.ChosenDisplayName, b.ChosenDisplayName);
        }

        [Test]
        public void Create_EmptyNamePool_FallsBackToArchetypeId()
        {
            var factory = new ActorInstanceFactory(new DeterministicRandom(1));

            var instance = factory.Create(Archetype());

            Assert.AreEqual("arch_road_bandit", instance.ChosenDisplayName);
        }

        [Test]
        public void Create_InstanceIds_AreUniqueAcrossPlacements()
        {
            var factory = new ActorInstanceFactory(new DeterministicRandom(1));
            var archetype = Archetype("Razor");

            var ids = new HashSet<string>();
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(ids.Add(factory.Create(archetype).InstanceId), "instance id must be unique per placement");
            }
        }

        [Test]
        public void Create_NullArchetype_ReturnsNull()
        {
            var factory = new ActorInstanceFactory(new DeterministicRandom(1));
            Assert.IsNull(factory.Create(null));
        }
    }
}
