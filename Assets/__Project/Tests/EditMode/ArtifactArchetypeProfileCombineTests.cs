using System.Collections.Generic;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ArtifactArchetypeProfileCombineTests
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
        public void Combine_Null_ReturnsEmpty()
        {
            var combined = ArtifactArchetypeProfile.Combine(null);

            Assert.IsTrue(combined.IsEmpty);
        }

        [Test]
        public void Combine_EmptySequence_ReturnsEmpty()
        {
            var combined = ArtifactArchetypeProfile.Combine(new List<ArtifactArchetypeProfile>());

            Assert.IsTrue(combined.IsEmpty);
        }

        [Test]
        public void Combine_SumsWeightsAcrossProfiles()
        {
            var combined = ArtifactArchetypeProfile.Combine(new[]
            {
                Profile(("reptile", 1f), ("aquatic", 0.5f)),
                Profile(("insect", 2f))
            });

            Assert.AreEqual(1f, combined.WeightFor("reptile"));
            Assert.AreEqual(0.5f, combined.WeightFor("aquatic"));
            Assert.AreEqual(2f, combined.WeightFor("insect"));
        }

        [Test]
        public void Combine_DuplicateIdsAcrossProfiles_AreSummed()
        {
            var combined = ArtifactArchetypeProfile.Combine(new[]
            {
                Profile(("reptile", 1f)),
                Profile(("reptile", 0.25f))
            });

            Assert.AreEqual(1.25f, combined.WeightFor("reptile"));
        }

        [Test]
        public void Combine_SkipsNullAndEmptyProfiles()
        {
            var combined = ArtifactArchetypeProfile.Combine(new[]
            {
                null,
                ArtifactArchetypeProfile.Empty,
                Profile(("mammal", 3f))
            });

            Assert.AreEqual(1, combined.Weights.Count);
            Assert.AreEqual(3f, combined.WeightFor("mammal"));
        }
    }
}
