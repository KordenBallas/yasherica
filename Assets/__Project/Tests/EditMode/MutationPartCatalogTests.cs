using System.Collections.Generic;
using System.Reflection;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Mutation.Core;
using Mutation.Data;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class MutationPartCatalogTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"Field '{field}' not found on {target.GetType().Name}.");
            info.SetValue(target, value);
        }

        private SlotDefinition NewSlot(string id)
        {
            var slot = ScriptableObject.CreateInstance<SlotDefinition>();
            _created.Add(slot);
            SetPrivate(slot, "_id", id);
            return slot;
        }

        private PartDefinition NewPart(
            string id,
            SlotDefinition slot = null,
            MutationRarity rarity = MutationRarity.Common,
            Sprite icon = null,
            params TraitAffinity[] traitAffinities)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            part.name = id;
            _created.Add(part);
            SetPrivate(part, "_id", id);
            SetPrivate(part, "_slot", slot);
            SetPrivate(part, "_rarity", rarity);
            SetPrivate(part, "_choiceIcon", icon);
            SetPrivate(part, "_traitAffinities", new List<TraitAffinity>(traitAffinities));
            return part;
        }

        private IPartCatalog PartCatalog(params PartDefinition[] parts)
        {
            return new PartCatalog(parts);
        }

        private static MutationCandidatePart Find(IMutationPartCatalog catalog, string partId)
        {
            foreach (var candidate in catalog.AllCandidates)
            {
                if (candidate.PartId == partId)
                {
                    return candidate;
                }
            }

            return null;
        }

        [Test]
        public void MapsSlotPartRarityAndTraitAffinity()
        {
            var slot = NewSlot("slot.head");
            var part = NewPart("part.head.r", slot, MutationRarity.Rare, null,
                new TraitAffinity("sharp", 0.8f),
                new TraitAffinity("toxic", 0.2f));

            var catalog = new MutationPartCatalog(PartCatalog(part));
            var candidate = Find(catalog, "part.head.r");

            Assert.NotNull(candidate);
            Assert.AreEqual("slot.head", candidate.SlotId);
            Assert.AreEqual("part.head.r", candidate.DisplayName); // no authored name -> asset name fallback
            Assert.AreEqual((int)MutationRarity.Rare, candidate.RarityTier);
            Assert.AreEqual(0.8f, candidate.TraitAffinity["sharp"]);
            Assert.AreEqual(0.2f, candidate.TraitAffinity["toxic"]);
        }

        [Test]
        public void UsesAuthoredDisplayName_WhenSet()
        {
            var part = NewPart("part.head.b", NewSlot("slot.head"), MutationRarity.Common, null,
                new TraitAffinity("sharp", 1f));
            SetPrivate(part, "_displayName", "Reptilian Head");

            var catalog = new MutationPartCatalog(PartCatalog(part));

            Assert.AreEqual("Reptilian Head", Find(catalog, "part.head.b").DisplayName);
        }

        [Test]
        public void FallsBackToAssetName_WhenDisplayNameBlank()
        {
            var part = NewPart("part.plain", NewSlot("slot.head"), MutationRarity.Common, null,
                new TraitAffinity("sharp", 1f));
            part.name = "Part_Plain_Asset";
            SetPrivate(part, "_displayName", "");

            var catalog = new MutationPartCatalog(PartCatalog(part));

            Assert.AreEqual("Part_Plain_Asset", Find(catalog, "part.plain").DisplayName);
        }

        [Test]
        public void DropsEmptyIdsAndNonPositiveWeightsAndSumsDuplicates()
        {
            var part = NewPart("part.x", null, MutationRarity.Common, null,
                new TraitAffinity("sharp", 0.4f),
                new TraitAffinity("sharp", 0.3f),  // summed -> 0.7
                new TraitAffinity("", 0.5f),       // empty id dropped
                new TraitAffinity("toxic", 0f));   // non-positive dropped

            var catalog = new MutationPartCatalog(PartCatalog(part));
            var candidate = Find(catalog, "part.x");

            Assert.AreEqual(1, candidate.TraitAffinity.Count);
            Assert.AreEqual(0.7f, candidate.TraitAffinity["sharp"], 0.0001f);
        }

        [Test]
        public void NoTraitAffinities_YieldsEmptyMap()
        {
            var part = NewPart("part.plain");

            var catalog = new MutationPartCatalog(PartCatalog(part));

            Assert.AreEqual(0, Find(catalog, "part.plain").TraitAffinity.Count);
        }

        [Test]
        public void TryGetIconReturnsAuthoredIcon()
        {
            var icon = Sprite.Create(
                Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            _created.Add(icon);
            var withIcon = NewPart("part.icon", null, MutationRarity.Common, icon);
            var withoutIcon = NewPart("part.plain");

            var catalog = new MutationPartCatalog(PartCatalog(withIcon, withoutIcon));

            Assert.IsTrue(catalog.TryGetIcon("part.icon", out var resolved));
            Assert.AreSame(icon, resolved);
            Assert.IsFalse(catalog.TryGetIcon("part.plain", out _));
            Assert.IsFalse(catalog.TryGetIcon("part.unknown", out _));
        }

        [Test]
        public void NullPartCatalog_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new MutationPartCatalog(null));
        }
    }
}
