using System.Collections.Generic;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MutationTallyTests
    {
        private static ArtifactArchetypeProfile Profile(params (string id, float weight)[] entries)
        {
            var pairs = new List<KeyValuePair<string, float>>();
            foreach (var entry in entries)
            {
                pairs.Add(new KeyValuePair<string, float>(entry.id, entry.weight));
            }

            return ArtifactArchetypeProfile.Create(pairs);
        }

        [Test]
        public void NewTally_IsEmpty()
        {
            var tally = new MutationTally();

            Assert.IsTrue(tally.IsEmpty);
            Assert.AreEqual(0, tally.Totals.Count);
            CollectionAssert.IsEmpty(tally.Dominant(3));
        }

        [Test]
        public void Add_SingleProfile_AccumulatesAndRaisesChanged()
        {
            var tally = new MutationTally();
            var changed = 0;
            tally.OnChanged += () => changed++;

            tally.Add(Profile(("reptile", 1f), ("aquatic", 0.5f)));

            Assert.IsFalse(tally.IsEmpty);
            Assert.AreEqual(1f, tally.TotalFor("reptile"));
            Assert.AreEqual(0.5f, tally.TotalFor("aquatic"));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Add_MultipleProfiles_SumsPerArchetype()
        {
            var tally = new MutationTally();

            tally.Add(Profile(("reptile", 1f), ("aquatic", 0.5f)));
            tally.Add(Profile(("reptile", 0.25f), ("insect", 2f)));

            Assert.AreEqual(1.25f, tally.TotalFor("reptile"));
            Assert.AreEqual(0.5f, tally.TotalFor("aquatic"));
            Assert.AreEqual(2f, tally.TotalFor("insect"));
        }

        [Test]
        public void Add_NullOrEmptyProfile_IsNoOpAndDoesNotRaiseChanged()
        {
            var tally = new MutationTally();
            var changed = 0;
            tally.OnChanged += () => changed++;

            tally.Add(null);
            tally.Add(ArtifactArchetypeProfile.Empty);

            Assert.IsTrue(tally.IsEmpty);
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void TotalFor_UnknownEmptyOrNullId_ReturnsZero()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 1f)));

            Assert.AreEqual(0f, tally.TotalFor("mammal"));
            Assert.AreEqual(0f, tally.TotalFor(""));
            Assert.AreEqual(0f, tally.TotalFor(null));
        }

        [Test]
        public void Dominant_ReturnsTopNByWeightDescending()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 3f), ("aquatic", 1f), ("insect", 2f)));

            CollectionAssert.AreEqual(new[] { "reptile", "insect" }, tally.Dominant(2));
        }

        [Test]
        public void Dominant_BreaksTiesByOrdinalId()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 1f), ("aquatic", 1f), ("insect", 1f)));

            CollectionAssert.AreEqual(new[] { "aquatic", "insect", "reptile" }, tally.Dominant(3));
        }

        [Test]
        public void Dominant_ClampsCountToTrackedArchetypes()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 2f), ("aquatic", 1f)));

            Assert.AreEqual(2, tally.Dominant(10).Count);
        }

        [Test]
        public void Dominant_NonPositiveCount_ReturnsEmpty()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 1f)));

            CollectionAssert.IsEmpty(tally.Dominant(0));
            CollectionAssert.IsEmpty(tally.Dominant(-1));
        }

        [Test]
        public void Reset_ClearsTotalsAndRaisesChanged_WhenNonEmpty()
        {
            var tally = new MutationTally();
            tally.Add(Profile(("reptile", 1f)));
            var changed = 0;
            tally.OnChanged += () => changed++;

            tally.Reset();

            Assert.IsTrue(tally.IsEmpty);
            Assert.AreEqual(0f, tally.TotalFor("reptile"));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Reset_WhenAlreadyEmpty_DoesNotRaiseChanged()
        {
            var tally = new MutationTally();
            var changed = 0;
            tally.OnChanged += () => changed++;

            tally.Reset();

            Assert.AreEqual(0, changed);
        }
    }
}
