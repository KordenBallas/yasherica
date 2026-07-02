using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ArtifactTraitProfileTests
    {
        [Test]
        public void Create_DropsNullAndEmptyIds()
        {
            var profile = ArtifactTraitProfile.Create(new[] { "sharp", null, "", "heavy" }, 1);

            Assert.AreEqual(2, profile.Traits.Count);
            Assert.IsTrue(profile.Has("sharp"));
            Assert.IsTrue(profile.Has("heavy"));
        }

        [Test]
        public void Create_DeduplicatesIds()
        {
            var profile = ArtifactTraitProfile.Create(new[] { "toxic", "toxic", "toxic" }, 0);

            Assert.AreEqual(1, profile.Traits.Count);
        }

        [Test]
        public void Create_OrdersTraitsOrdinally()
        {
            var profile = ArtifactTraitProfile.Create(new[] { "zeta", "alpha", "mid" }, 0);

            Assert.AreEqual("alpha", profile.Traits[0]);
            Assert.AreEqual("mid", profile.Traits[1]);
            Assert.AreEqual("zeta", profile.Traits[2]);
        }

        [Test]
        public void Create_ClampsNegativeTierToZero()
        {
            var profile = ArtifactTraitProfile.Create(new[] { "sharp" }, -3);

            Assert.AreEqual(0, profile.Tier);
        }

        [Test]
        public void Create_NullSource_YieldsEmptyTraits()
        {
            var profile = ArtifactTraitProfile.Create(null, 2);

            Assert.AreEqual(0, profile.Traits.Count);
            Assert.AreEqual(2, profile.Tier);
        }

        [Test]
        public void Has_UnknownOrEmptyId_ReturnsFalse()
        {
            var profile = ArtifactTraitProfile.Create(new[] { "sharp" }, 0);

            Assert.IsFalse(profile.Has("heavy"));
            Assert.IsFalse(profile.Has(null));
            Assert.IsFalse(profile.Has(""));
        }

        [Test]
        public void Empty_HasNoTraitsAndTierZero()
        {
            Assert.AreEqual(0, ArtifactTraitProfile.Empty.Traits.Count);
            Assert.AreEqual(0, ArtifactTraitProfile.Empty.Tier);
        }
    }
}
