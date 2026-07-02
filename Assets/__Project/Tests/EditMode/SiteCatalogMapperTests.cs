using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using World.Sites.Core;
using World.Sites.Data;

namespace Tests.EditMode
{
    [TestFixture]
    public class SiteCatalogMapperTests
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Flags).SetValue(target, value);

        private static ContentBeatEntry Beat(ContentBaseKind kind, string flavor)
        {
            var entry = new ContentBeatEntry();
            Set(entry, "_kind", kind);
            Set(entry, "_flavor", flavor);
            return entry;
        }

        private static WeightedBeatEntry Row(ContentBaseKind kind, string flavor, int weight)
        {
            var entry = new WeightedBeatEntry();
            Set(entry, "_kind", kind);
            Set(entry, "_flavor", flavor);
            Set(entry, "_weight", weight);
            return entry;
        }

        private SiteFamilyDefinition Family(string id, ContentBeatEntry anchor,
            int fillMin, int fillMax, params WeightedBeatEntry[] fill)
        {
            var family = ScriptableObject.CreateInstance<SiteFamilyDefinition>();
            _created.Add(family);
            Set(family, "_familyId", id);
            Set(family, "_defaultAnchorBeats", new List<ContentBeatEntry> { anchor });
            Set(family, "_defaultFillBudgetMin", fillMin);
            Set(family, "_defaultFillBudgetMax", fillMax);
            Set(family, "_defaultFillTable", new List<WeightedBeatEntry>(fill));
            return family;
        }

        private SiteDefinition Site(string id, SiteFamilyDefinition family,
            int footprintMin = 2, int footprintMax = 3, int weight = 1)
        {
            var site = ScriptableObject.CreateInstance<SiteDefinition>();
            _created.Add(site);
            Set(site, "_siteId", id);
            Set(site, "_family", family);
            Set(site, "_footprintMin", footprintMin);
            Set(site, "_footprintMax", footprintMax);
            Set(site, "_triggerWeight", weight);
            Set(site, "_dressingThemeId", id + "-kit");
            return site;
        }

        private SiteFamilyDefinition Settlement() =>
            Family("settlement", Beat(ContentBaseKind.Npc, "quest-bearer"), 1, 1,
                Row(ContentBaseKind.Npc, "townsfolk", 3), Row(ContentBaseKind.Loot, "scattered", 1));

        [Test]
        public void SiteWithoutOverrides_InheritsTheFamilyDefaultRecipe()
        {
            var catalog = SiteCatalogMapper.ToCatalog(new[] { Site("village", Settlement()) });

            var village = catalog.Get("village");
            Assert.IsNotNull(village);
            Assert.AreEqual("settlement", village.FamilyId);
            Assert.AreEqual(ContentBaseKind.Npc, village.AnchorBeats[0].Kind);
            Assert.AreEqual("quest-bearer", village.AnchorBeats[0].Flavor);
            Assert.AreEqual(1, village.FillBudgetMin);
            Assert.AreEqual(1, village.FillBudgetMax);
            Assert.AreEqual(2, village.FillTable.Count);
            Assert.AreEqual("village-kit", village.DressingThemeId);
        }

        [Test]
        public void OverrideToggles_ReplaceOnlyTheirDelta()
        {
            var site = Site("city", Settlement(), footprintMin: 4, footprintMax: 5);
            Set(site, "_overrideFillBudget", true);
            Set(site, "_fillBudgetMin", 2);
            Set(site, "_fillBudgetMax", 3);
            Set(site, "_overrideFillTable", true);
            Set(site, "_fillTable", new List<WeightedBeatEntry>
            {
                Row(ContentBaseKind.Npc, "townsfolk", 5),
                Row(ContentBaseKind.Loot, "market", 3)
            });

            var city = SiteCatalogMapper.ToCatalog(new[] { site }).Get("city");

            // Anchors stay inherited; budget and table are the site's own.
            Assert.AreEqual("quest-bearer", city.AnchorBeats[0].Flavor);
            Assert.AreEqual(2, city.FillBudgetMin);
            Assert.AreEqual(3, city.FillBudgetMax);
            Assert.AreEqual("market", city.FillTable[1].Beat.Flavor);
        }

        [Test]
        public void TriggerChannel_DerivesFromTheOverriddenAnchor_NotTheFamily()
        {
            // Camp: family Settlement (NPC anchor) but its own anchor is Combat·bandit -> ambient channel.
            var camp = Site("camp", Settlement(), footprintMin: 1, footprintMax: 2);
            Set(camp, "_overrideAnchorBeats", true);
            Set(camp, "_anchorBeats", new List<ContentBeatEntry> { Beat(ContentBaseKind.Combat, "bandit") });

            var catalog = SiteCatalogMapper.ToCatalog(new[] { camp, Site("village", Settlement()) });

            Assert.AreEqual(SiteTriggerChannel.Ambient, catalog.Get("camp").TriggerChannel);
            Assert.AreEqual(SiteTriggerChannel.Quest, catalog.Get("village").TriggerChannel);
            Assert.AreEqual(1, catalog.AmbientSites.Count);
            Assert.AreEqual(1, catalog.QuestSites.Count);
        }

        [Test]
        public void SiteWithNoAnchor_NoFamilyAndNoOverride_IsSkipped()
        {
            var orphan = Site("orphan", family: null);

            var catalog = SiteCatalogMapper.ToCatalog(new[] { orphan });

            Assert.IsNull(catalog.Get("orphan"));
            Assert.AreEqual(0, catalog.QuestSites.Count + catalog.AmbientSites.Count);
        }

        [Test]
        public void MissingId_AndDuplicateId_AreSkipped()
        {
            var family = Settlement();
            var noId = Site(string.Empty, family);
            var first = Site("village", family);
            var duplicate = Site("village", family, footprintMin: 9, footprintMax: 9);

            var catalog = SiteCatalogMapper.ToCatalog(new[] { noId, first, duplicate });

            Assert.AreEqual(1, catalog.QuestSites.Count);
            Assert.AreEqual(2, catalog.Get("village").FootprintMin, "The first authored asset wins.");
        }

        [Test]
        public void NpcFillFlavors_CollectFromEffectiveFillTables()
        {
            var catalog = SiteCatalogMapper.ToCatalog(new[] { Site("village", Settlement()) });

            CollectionAssert.Contains(catalog.NpcFillFlavors, "townsfolk");
            CollectionAssert.DoesNotContain(catalog.NpcFillFlavors, "quest-bearer");
            CollectionAssert.DoesNotContain(catalog.NpcFillFlavors, "scattered");
        }

        [Test]
        public void NullAndEmptyInput_ProduceAnEmptyCatalog()
        {
            Assert.AreEqual(0, SiteCatalogMapper.ToCatalog(null).QuestSites.Count);
            Assert.AreEqual(0, SiteCatalogMapper.ToCatalog(new SiteDefinition[0]).AmbientSites.Count);
        }
    }
}
