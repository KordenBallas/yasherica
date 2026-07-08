using CharacterSystem.Data.Definitions;
using Inventory.Data.Definitions;
using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The FR16 demonstrator set, asserted over the REAL authored assets: a deed done in run N
    /// makes a previously unavailable token possible in run N+1 (the acceptance proof of the
    /// meta-progression loop), and unmarked sibling assets stay base (FR15).
    /// </summary>
    [TestFixture]
    public class MetaGatingDemonstratorTests
    {
        private static IMetaVocabulary Vocabulary(FactStoreSnapshot facts, int effectiveRunCount) =>
            new MetaVocabulary(facts, MetaProgressionSettings.Defaults, effectiveRunCount);

        private sealed class StubHeatLens : IHeatLens
        {
            public int CurrentHeat { get; set; }
            public int HighWaterHeat { get; set; }
            public int FloorReliefRuns { get; set; }
        }

        private static FactStoreSnapshot Facts(params FactEntryDto[] entries)
        {
            var snapshot = new FactStoreSnapshot();
            snapshot.Entries.AddRange(entries);
            return snapshot;
        }

        private static FactEntryDto BoolFact(string subject, string key) => new FactEntryDto
        {
            Namespace = FactNamespace.World,
            Subject = subject,
            Key = key,
            Type = FactValueType.Bool,
            BoolValue = true
        };

        [Test]
        public void SpiderLegs_AreGatedBehindRunThree()
        {
            var part = Resources.Load<PartDefinition>("CharacterSystem/Parts/Part_LegsSpider");
            Assert.IsNotNull(part, "demonstrator asset missing");
            var gate = part.MetaGating.ToCore();

            Assert.AreEqual(GatingMark.MetaGated, gate.Mark);
            Assert.IsFalse(Vocabulary(Facts(), 2).IsUnlocked(part.Id, gate), "locked before run 3");
            Assert.IsTrue(Vocabulary(Facts(), 3).IsUnlocked(part.Id, gate), "open from run 3");
        }

        [Test]
        public void SerpentSpine_UnlocksOnTheSpineMilestone()
        {
            var part = Resources.Load<PartDefinition>("CharacterSystem/Parts/Part_SpineSerpent");
            Assert.IsNotNull(part, "demonstrator asset missing");
            var gate = part.MetaGating.ToCore();

            Assert.AreEqual(GatingMark.MetaGated, gate.Mark);
            Assert.IsFalse(Vocabulary(Facts(), 9).IsUnlocked(part.Id, gate), "locked without the milestone");
            var seen = Facts(BoolFact("story_spine_cauldron_hint", "spine_seen"));
            Assert.IsTrue(Vocabulary(seen, 2).IsUnlocked(part.Id, gate), "run N milestone opens run N+1");
        }

        [Test]
        public void SnakeRecipe_UnlocksWhenTheSerpentIsTasted()
        {
            var recipe = Resources.Load<RecipeDefinition>("Artifacts/Recipes/Recipe_FireWater_Snake");
            Assert.IsNotNull(recipe, "demonstrator asset missing");
            var gate = recipe.MetaGating.ToCore();

            Assert.AreEqual(GatingMark.MetaGated, gate.Mark);
            Assert.IsFalse(Vocabulary(Facts(), 5).IsUnlocked(recipe.Output.Id, gate));
            var tasted = Facts(BoolFact("part.spine.serpent", "arena_tasted"));
            Assert.IsTrue(Vocabulary(tasted, 2).IsUnlocked(recipe.Output.Id, gate));
        }

        [Test]
        public void StingerRecipe_NeedsTheTasteAndAHotPact()
        {
            // The Track Y demonstrator: this recipe carries min-Heat 2 (keyed to the current pact)
            // on top of its taste deed — a reason to play hot.
            var recipe = Resources.Load<RecipeDefinition>("Artifacts/Recipes/Recipe_NeedleBacteria_Stinger");
            Assert.IsNotNull(recipe, "demonstrator asset missing");
            var gate = recipe.MetaGating.ToCore();

            Assert.AreEqual(GatingMark.MetaGated, gate.Mark);
            Assert.AreEqual(2, gate.MinHeat);
            Assert.AreEqual(HeatGateKey.CurrentPact, gate.HeatKey);

            var tasted = Facts(BoolFact("part.legs.spider", "arena_tasted"));
            Assert.IsFalse(Vocabulary(Facts(), 5).IsUnlocked(recipe.Output.Id, gate), "no taste, no heat");
            Assert.IsFalse(Vocabulary(tasted, 2).IsUnlocked(recipe.Output.Id, gate),
                "tasted but cold (no heat lens = heat 0): the min-Heat gate holds");
            var hot = new MetaVocabulary(tasted, MetaProgressionSettings.Defaults, 2,
                new StubHeatLens { CurrentHeat = 2 });
            Assert.IsTrue(hot.IsUnlocked(recipe.Output.Id, gate), "tasted + a hot enough pact opens it");
        }

        [Test]
        public void UnmarkedSiblings_StayBase()
        {
            // FR15: assets that never touched the gating block keep working, base by default.
            var part = Resources.Load<PartDefinition>("CharacterSystem/Parts/Part_Head_A");
            var recipe = Resources.Load<RecipeDefinition>("Artifacts/Recipes/Recipe_FireRock_Lizard");
            Assert.IsNotNull(part);
            Assert.IsNotNull(recipe);

            var emptyMeta = Vocabulary(Facts(), 1);
            Assert.AreEqual(GatingMark.Base, part.MetaGating.ToCore().Mark);
            Assert.AreEqual(GatingMark.Base, recipe.MetaGating.ToCore().Mark);
            Assert.IsTrue(emptyMeta.IsUnlocked(part.Id, part.MetaGating.ToCore()));
            Assert.IsTrue(emptyMeta.IsUnlocked(recipe.Output.Id, recipe.MetaGating.ToCore()));
        }
    }
}
