using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ArtifactByTraitSelectorTests
    {
        private static readonly FusionSettings Settings = new FusionSettings(1, 1f, 0.25f, 0.5f);

        private ArtifactByTraitSelector _selector;

        [SetUp]
        public void SetUp()
        {
            _selector = new ArtifactByTraitSelector();
        }

        private static ArtifactTraitProfile Target(int tier, params string[] traits)
        {
            return ArtifactTraitProfile.Create(traits, tier);
        }

        [Test]
        public void SelectBest_PicksHighestTraitOverlap()
        {
            var source = new FakeArtifactTraitSource()
                .Add("club", 0, "heavy")
                .Add("magma", 0, "fire", "stone");

            string best = _selector.SelectBest(Target(0, "fire", "stone"), source, null, Settings);

            Assert.AreEqual("magma", best);
        }

        [Test]
        public void SelectBest_OffTargetTraitsPenalize()
        {
            var source = new FakeArtifactTraitSource()
                .Add("clean", 0, "sharp")
                .Add("noisy", 0, "sharp", "rot", "heavy", "fire");

            string best = _selector.SelectBest(Target(0, "sharp"), source, null, Settings);

            Assert.AreEqual("clean", best);
        }

        [Test]
        public void SelectBest_CloserTierWinsWithEqualTraits()
        {
            var source = new FakeArtifactTraitSource()
                .Add("raw", 0, "toxic")
                .Add("refined", 2, "toxic");

            string best = _selector.SelectBest(Target(2, "toxic"), source, null, Settings);

            Assert.AreEqual("refined", best);
        }

        [Test]
        public void SelectBest_TieBreaksByOrdinalId()
        {
            var source = new FakeArtifactTraitSource()
                .Add("beta", 0, "sharp")
                .Add("alpha", 0, "sharp");

            string best = _selector.SelectBest(Target(0, "sharp"), source, null, Settings);

            Assert.AreEqual("alpha", best);
        }

        [Test]
        public void SelectBest_SkipsExcludedIds()
        {
            var source = new FakeArtifactTraitSource()
                .Add("input", 0, "fire", "stone")
                .Add("other", 0, "fire");

            string best = _selector.SelectBest(
                Target(0, "fire", "stone"), source, new[] { "input" }, Settings);

            Assert.AreEqual("other", best);
        }

        [Test]
        public void SelectBest_AllExcluded_FallsBackToFullPool()
        {
            var source = new FakeArtifactTraitSource().Add("only", 0, "fire");

            string best = _selector.SelectBest(Target(0, "fire"), source, new[] { "only" }, Settings);

            Assert.AreEqual("only", best);
        }

        [Test]
        public void SelectBest_EmptySource_ReturnsNull()
        {
            var source = new FakeArtifactTraitSource();

            string best = _selector.SelectBest(Target(0, "fire"), source, null, Settings);

            Assert.IsNull(best);
        }
    }
}
