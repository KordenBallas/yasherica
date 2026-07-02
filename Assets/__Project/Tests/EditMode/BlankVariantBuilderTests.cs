using System.Collections.Generic;
using Inventory.Core;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BlankVariantBuilderTests
    {
        private static readonly FusionSettings Fusion = new FusionSettings(1, 1f, 0.25f, 0.5f);
        private static readonly VariantScoringParameters Scoring = new VariantScoringParameters(0.5f, 1f);

        private static BlankVariantBuilder Builder(TraitFusionRuleSet rules = null)
        {
            return new BlankVariantBuilder(
                new EmergentFusionCalculator(), rules ?? TraitFusionRuleSet.Empty, Fusion);
        }

        private static PartBlankData Blank(string slotId = "slot.head", string species = "reptile", int sockets = 2)
        {
            return new PartBlankData("blank.x", "Blank X", slotId, species, sockets);
        }

        private static MutationCandidatePart Part(
            string partId, string slotId, int rarity, params (string id, float weight)[] traits)
        {
            var map = new Dictionary<string, float>();
            foreach (var (id, weight) in traits)
            {
                map[id] = weight;
            }

            return new MutationCandidatePart(slotId, partId, partId, rarity, map);
        }

        private static ArtifactTraitProfile Profile(int tier, params string[] traits)
        {
            return ArtifactTraitProfile.Create(traits, tier);
        }

        [Test]
        public void Build_RanksByTraitAffinityOverlap()
        {
            var options = Builder().Build(
                Blank(),
                new[] { Profile(0, "sharp", "toxic") },
                new[]
                {
                    Part("part.blunt", "slot.head", 0, ("heavy", 0.9f)),
                    Part("part.fang", "slot.head", 0, ("sharp", 0.7f), ("toxic", 0.5f))
                },
                null, 3, Scoring);

            Assert.AreEqual(2, options.Count);
            Assert.AreEqual("part.fang", options[0].PartId);
            Assert.AreEqual("part.blunt", options[1].PartId);
        }

        [Test]
        public void Build_FiltersToBlankSlot()
        {
            var options = Builder().Build(
                Blank("slot.head"),
                new[] { Profile(0, "sharp") },
                new[]
                {
                    Part("part.head", "slot.head", 0, ("sharp", 1f)),
                    Part("part.leg", "slot.leg.left", 0, ("sharp", 1f))
                },
                null, 3, Scoring);

            Assert.AreEqual(1, options.Count);
            Assert.AreEqual("part.head", options[0].PartId);
        }

        [Test]
        public void Build_ExcludesEquippedParts()
        {
            var options = Builder().Build(
                Blank(),
                new[] { Profile(0, "sharp") },
                new[]
                {
                    Part("part.a", "slot.head", 0, ("sharp", 1f)),
                    Part("part.b", "slot.head", 0, ("sharp", 0.5f))
                },
                new[] { "part.a" }, 3, Scoring);

            Assert.AreEqual(1, options.Count);
            Assert.AreEqual("part.b", options[0].PartId);
        }

        [Test]
        public void Build_ZeroScoringPartsStillOffered_RawIsWeakNeverDead()
        {
            var options = Builder().Build(
                Blank(),
                new[] { Profile(0, "water") },
                new[] { Part("part.a", "slot.head", 0, ("sharp", 1f)) },
                null, 3, Scoring);

            Assert.AreEqual(1, options.Count);
        }

        [Test]
        public void Build_SocketsInteract_FusionRuleUnlocksEmergentAffinity()
        {
            // Neither reagent carries 'beaming', but focusing+fiery transmute into it -
            // the part expressing 'beaming' outranks the literal matches.
            var rules = new TraitFusionRuleSet(new List<TraitFusionRule>
            {
                new TraitFusionRule("beam", new[] { "focusing", "fiery" }, new[] { "beaming" }, new[] { "fiery" }, 1)
            });

            var options = Builder(rules).Build(
                Blank(),
                new[] { Profile(0, "focusing"), Profile(0, "fiery") },
                new[]
                {
                    Part("part.laser", "slot.head", 0, ("beaming", 1f)),
                    Part("part.torch", "slot.head", 0, ("fiery", 0.9f))
                },
                null, 3, Scoring);

            Assert.AreEqual("part.laser", options[0].PartId);
        }

        [Test]
        public void Build_HighTierSockets_UnlockRareParts()
        {
            var common = Part("part.common", "slot.head", 0, ("sharp", 0.6f));
            var rare = Part("part.rare", "slot.head", 3, ("sharp", 0.5f));

            // Raw (tier 0) sockets: the rare multiplier is locked, common's higher overlap wins.
            var rawOptions = Builder().Build(
                Blank(), new[] { Profile(0, "sharp") },
                new[] { common, rare }, null, 3, Scoring);
            Assert.AreEqual("part.common", rawOptions[0].PartId);

            // Crafted (tier 3) sockets: the rarity multiplier fully unlocks and the rare part wins.
            var craftedOptions = Builder().Build(
                Blank(), new[] { Profile(3, "sharp") },
                new[] { common, rare }, null, 3, Scoring);
            Assert.AreEqual("part.rare", craftedOptions[0].PartId);
        }

        [Test]
        public void Build_CapsAtMaxOptions_AndBreaksTiesOrdinally()
        {
            var options = Builder().Build(
                Blank(),
                new[] { Profile(0, "sharp") },
                new[]
                {
                    Part("part.b", "slot.head", 0, ("sharp", 1f)),
                    Part("part.a", "slot.head", 0, ("sharp", 1f)),
                    Part("part.c", "slot.head", 0, ("sharp", 1f))
                },
                null, 2, Scoring);

            Assert.AreEqual(2, options.Count);
            Assert.AreEqual("part.a", options[0].PartId);
            Assert.AreEqual("part.b", options[1].PartId);
        }

        [Test]
        public void Build_CarriesBlankSlotAndSpeciesOntoOptions()
        {
            var options = Builder().Build(
                Blank("slot.head", "reptile"),
                new[] { Profile(0, "sharp") },
                new[] { Part("part.a", "slot.head", 0, ("sharp", 1f)) },
                null, 3, Scoring);

            Assert.AreEqual("slot.head", options[0].SlotId);
            // The card tint comes from the blank's passport marker, not the part.
            Assert.AreEqual("reptile", options[0].ArchetypeId);
        }

        [Test]
        public void Build_NullBlankOrNoCandidates_YieldsEmpty()
        {
            Assert.AreEqual(0, Builder().Build(
                null, new[] { Profile(0, "sharp") },
                new MutationCandidatePart[0], null, 3, Scoring).Count);
            Assert.AreEqual(0, Builder().Build(
                Blank(), new[] { Profile(0, "sharp") },
                new MutationCandidatePart[0], null, 3, Scoring).Count);
        }
    }
}
