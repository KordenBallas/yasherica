using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FusionResolverTests
    {
        private static readonly FusionSettings Settings = new FusionSettings(1, 1f, 0.25f, 0.5f);

        private static FusionResolver CreateResolver(
            IArtifactTraitSource source,
            IReadOnlyList<RecipeData> recipes = null,
            TraitFusionRuleSet rules = null)
        {
            return new FusionResolver(
                new RecipeBook(recipes ?? new List<RecipeData>()),
                source,
                new EmergentFusionCalculator(),
                new ArtifactByTraitSelector(),
                rules ?? TraitFusionRuleSet.Empty,
                Settings);
        }

        [Test]
        public void Resolve_SignatureRecipe_WinsOverEmergent()
        {
            var source = new FakeArtifactTraitSource()
                .Add("fire", 0, "fire", "fiery")
                .Add("water", 0, "water")
                .Add("steam", 0, "fire", "water");
            var recipes = new List<RecipeData> { new RecipeData(new[] { "fire", "water" }, "snake") };

            var result = CreateResolver(source, recipes).Resolve(new[] { "fire", "water" });

            Assert.AreEqual("snake", result.OutputDefinitionId);
            Assert.IsTrue(result.IsSignature);
        }

        [Test]
        public void Resolve_NoSignature_PicksBestNonInputMatch()
        {
            var source = new FakeArtifactTraitSource()
                .Add("fire", 0, "fire", "fiery")
                .Add("rock", 0, "stone", "heavy")
                .Add("magma", 1, "fire", "stone");

            var result = CreateResolver(source).Resolve(new[] { "fire", "rock" });

            Assert.AreEqual("magma", result.OutputDefinitionId);
            Assert.IsFalse(result.IsSignature);
        }

        [Test]
        public void Resolve_FusionRuleSteersTheTarget()
        {
            // focusing + fiery transmute into beaming; only "beam" carries it.
            var source = new FakeArtifactTraitSource()
                .Add("lens", 0, "focusing")
                .Add("coal", 0, "fire", "fiery")
                .Add("beam", 1, "beaming", "focusing")
                .Add("pebble", 0, "stone");
            var rules = new TraitFusionRuleSet(new List<TraitFusionRule>
            {
                new TraitFusionRule("beam_rule", new[] { "focusing", "fiery" }, new[] { "beaming" }, new[] { "fiery" }, 1)
            });

            var result = CreateResolver(source, rules: rules).Resolve(new[] { "lens", "coal" });

            Assert.AreEqual("beam", result.OutputDefinitionId);
        }

        [Test]
        public void Resolve_EmptyTraitSource_FallsBackToLastInput()
        {
            var result = CreateResolver(new FakeArtifactTraitSource()).Resolve(new[] { "fire", "rock" });

            Assert.AreEqual("rock", result.OutputDefinitionId);
            Assert.IsFalse(result.IsSignature);
        }

        [Test]
        public void Resolve_NoInputs_Throws()
        {
            var resolver = CreateResolver(new FakeArtifactTraitSource());

            Assert.Throws<System.ArgumentException>(() => resolver.Resolve(new string[0]));
            Assert.Throws<System.ArgumentException>(() => resolver.Resolve(null));
        }
    }
}
