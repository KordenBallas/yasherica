using System.Collections.Generic;
using Mutation.Data;
using Mutation.Data.Definitions;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ArtifactArchetypeMapperTests
    {
        [Test]
        public void ToProfile_Null_ReturnsEmpty()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(null);

            Assert.IsTrue(profile.IsEmpty);
        }

        [Test]
        public void ToProfile_EmptyList_ReturnsEmpty()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new List<ArchetypeWeight>());

            Assert.IsTrue(profile.IsEmpty);
        }

        [Test]
        public void ToProfile_PreservesWeights()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new[]
            {
                new ArchetypeWeight("reptile", 1f),
                new ArchetypeWeight("aquatic", 0.3f)
            });

            Assert.AreEqual(2, profile.Weights.Count);
            Assert.AreEqual(1f, profile.WeightFor("reptile"));
            Assert.AreEqual(0.3f, profile.WeightFor("aquatic"));
        }

        [Test]
        public void ToProfile_SumsDuplicateIds()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new[]
            {
                new ArchetypeWeight("reptile", 0.5f),
                new ArchetypeWeight("reptile", 0.25f)
            });

            Assert.AreEqual(1, profile.Weights.Count);
            Assert.AreEqual(0.75f, profile.WeightFor("reptile"));
        }

        [Test]
        public void ToProfile_DropsEmptyIdsAndNonPositiveWeights()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new[]
            {
                new ArchetypeWeight("", 1f),
                new ArchetypeWeight(null, 1f),
                new ArchetypeWeight("mammal", 0f),
                new ArchetypeWeight("avian", -2f),
                new ArchetypeWeight("insect", 0.4f)
            });

            Assert.AreEqual(1, profile.Weights.Count);
            Assert.AreEqual(0.4f, profile.WeightFor("insect"));
            Assert.AreEqual(0f, profile.WeightFor("mammal"));
        }

        [Test]
        public void ToProfile_TrimsWhitespaceInIds()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new[]
            {
                new ArchetypeWeight("  reptile  ", 0.5f),
                new ArchetypeWeight("reptile", 0.5f)
            });

            Assert.AreEqual(1, profile.Weights.Count);
            Assert.AreEqual(1f, profile.WeightFor("reptile"));
        }

        [Test]
        public void ToProfile_SkipsNullEntries()
        {
            var profile = ArtifactArchetypeMapper.ToProfile(new[]
            {
                null,
                new ArchetypeWeight("reptile", 1f)
            });

            Assert.AreEqual(1, profile.Weights.Count);
            Assert.AreEqual(1f, profile.WeightFor("reptile"));
        }
    }
}
