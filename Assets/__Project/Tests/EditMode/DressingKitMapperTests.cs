using System.Collections.Generic;
using System.Reflection;
using LevelGeneration;
using NUnit.Framework;
using UnityEngine;
using World.Biomes.Data;
using World.Dressing.Core;
using World.Dressing.Data;

namespace Tests.EditMode
{
    [TestFixture]
    public class DressingKitMapperTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static void Set(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, PrivateInstance)
                .SetValue(target, value);
        }

        private static object MakeEntry(
            GameObject prefab, FeatureKind kind, int weight,
            float footprintOverride = 0f, bool mayOverhang = false)
        {
            var entry = new BiomeFeatureKitDefinition.FeatureEntry();
            Set(entry, "_prefab", prefab);
            Set(entry, "_kind", kind);
            Set(entry, "_weight", weight);
            Set(entry, "_scaleMin", 0.8f);
            Set(entry, "_scaleMax", 1.2f);
            Set(entry, "_footprintOverride", footprintOverride);
            Set(entry, "_mayOverhang", mayOverhang);
            return entry;
        }

        private static BiomeFeatureKitDefinition MakeBiomeKit(
            string kitId, params object[] entries)
        {
            var kit = ScriptableObject.CreateInstance<BiomeFeatureKitDefinition>();
            Set(kit, "_kitId", kitId);
            var list = new List<BiomeFeatureKitDefinition.FeatureEntry>();
            foreach (var entry in entries)
            {
                list.Add((BiomeFeatureKitDefinition.FeatureEntry)entry);
            }

            Set(kit, "_features", list);
            return kit;
        }

        private static BiomeAppearanceDefinition MakeAppearance(
            LevelTheme theme, BiomeFeatureKitDefinition kit)
        {
            var appearance = ScriptableObject.CreateInstance<BiomeAppearanceDefinition>();
            Set(appearance, "_theme", theme);
            Set(appearance, "_featureKit", kit);
            Set(appearance, "_blockersPer100Cells", 5f);
            Set(appearance, "_decorClustersPer100Cells", 12f);
            Set(appearance, "_laneHalfWidthCells", 1.5f);
            return appearance;
        }

        private static SiteDressingKitDefinition MakeSiteKit(
            string themeId, int structures, int props, int focal, int gates)
        {
            var kit = ScriptableObject.CreateInstance<SiteDressingKitDefinition>();
            Set(kit, "_kitId", $"kit-{themeId}");
            Set(kit, "_dressingThemeId", themeId);
            Set(kit, "_structures", FilledList(structures));
            Set(kit, "_props", FilledList(props));
            Set(kit, "_focalProps", FilledList(focal));
            Set(kit, "_gateProps", FilledList(gates));
            return kit;
        }

        private static List<GameObject> FilledList(int count)
        {
            var list = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new GameObject($"stub_{i}"));
            }

            return list;
        }

        [Test]
        public void BiomeCatalog_MapsKitAndDensityFromTheAppearance()
        {
            var kit = MakeBiomeKit(
                "forest-kit",
                MakeEntry(new GameObject("tree"), FeatureKind.Blocking, 3),
                MakeEntry(new GameObject("tuft"), FeatureKind.SmallDecorative, 5));
            var catalog = DressingKitMapper.ToBiomeCatalog(
                new[] { MakeAppearance(LevelTheme.Forest, kit) });

            Assert.IsTrue(catalog.TryGet(LevelTheme.Forest, out var pool, out var density));
            Assert.AreEqual("forest-kit", pool.KitId);
            Assert.AreEqual(2, pool.Entries.Count);
            Assert.AreEqual(FeatureKind.Blocking, pool.Entries[0].Kind);
            Assert.AreEqual(3, pool.Entries[0].Weight);
            Assert.AreEqual(5f, density.BlockersPer100Cells);
            Assert.AreEqual(12f, density.DecorClustersPer100Cells);
            Assert.AreEqual(1.5f, density.LaneHalfWidth);
        }

        [Test]
        public void BiomeCatalog_UnboundBiome_IsAbsent()
        {
            var catalog = DressingKitMapper.ToBiomeCatalog(
                new[] { MakeAppearance(LevelTheme.Mountain, null) });
            Assert.IsFalse(catalog.TryGet(LevelTheme.Mountain, out _, out _));
            Assert.IsFalse(catalog.TryGet(LevelTheme.Forest, out _, out _));
        }

        [Test]
        public void BiomeCatalog_NullPrefabEntry_KeepsItsSlotAtWeightZero()
        {
            // Index alignment is the swap contract: a broken entry must not shift later indices.
            var kit = MakeBiomeKit(
                "forest-kit",
                MakeEntry(null, FeatureKind.Blocking, 3),
                MakeEntry(new GameObject("tuft"), FeatureKind.SmallDecorative, 5));
            var catalog = DressingKitMapper.ToBiomeCatalog(
                new[] { MakeAppearance(LevelTheme.Forest, kit) });

            Assert.IsTrue(catalog.TryGet(LevelTheme.Forest, out var pool, out _));
            Assert.AreEqual(2, pool.Entries.Count);
            Assert.AreEqual(0, pool.Entries[0].Weight, "Broken entry keeps its slot, never drawn.");
            Assert.AreEqual(5, pool.Entries[1].Weight);
        }

        [Test]
        public void BiomeCatalog_ResolvesFootprintAndOverhang()
        {
            // Zero override = the kind default (light authoring, edge-fit brief FR2); an override
            // and the may-overhang flag carry through verbatim.
            var kit = MakeBiomeKit(
                "forest-kit",
                MakeEntry(new GameObject("tuft"), FeatureKind.SmallDecorative, 1),
                MakeEntry(new GameObject("rock"), FeatureKind.LargeDecorative, 1, footprintOverride: 1.7f),
                MakeEntry(new GameObject("tree"), FeatureKind.LargeDecorative, 1, mayOverhang: true));
            var catalog = DressingKitMapper.ToBiomeCatalog(
                new[] { MakeAppearance(LevelTheme.Forest, kit) });

            Assert.IsTrue(catalog.TryGet(LevelTheme.Forest, out var pool, out _));
            Assert.AreEqual(FeatureEntryData.SmallFootprintDefault, pool.Entries[0].FootprintRadius);
            Assert.IsFalse(pool.Entries[0].MayOverhang);
            Assert.AreEqual(1.7f, pool.Entries[1].FootprintRadius);
            Assert.AreEqual(FeatureEntryData.LargeFootprintDefault, pool.Entries[2].FootprintRadius);
            Assert.IsTrue(pool.Entries[2].MayOverhang);
        }

        [Test]
        public void SiteCatalog_MapsRoleCountsByThemeId()
        {
            var catalog = DressingKitMapper.ToSiteCatalog(
                new[] { MakeSiteKit("settlement-kit", 5, 3, 0, 2) });

            Assert.IsTrue(catalog.TryGet("settlement-kit", out var kit));
            Assert.AreEqual(5, kit.StructureCount);
            Assert.AreEqual(3, kit.PropCount);
            Assert.AreEqual(0, kit.FocalCount);
            Assert.AreEqual(2, kit.GateCount);
            Assert.IsFalse(catalog.TryGet("unknown", out _));
        }

        [Test]
        public void SiteCatalog_DuplicateThemeId_FirstWins()
        {
            var catalog = DressingKitMapper.ToSiteCatalog(new[]
            {
                MakeSiteKit("camp-kit", 0, 4, 1, 0),
                MakeSiteKit("camp-kit", 9, 9, 9, 9)
            });

            Assert.IsTrue(catalog.TryGet("camp-kit", out var kit));
            Assert.AreEqual(4, kit.PropCount, "First authored kit wins.");
        }

        [Test]
        public void NullInputs_YieldEmptyCatalogs()
        {
            Assert.IsFalse(DressingKitMapper.ToBiomeCatalog(null).TryGet(LevelTheme.Forest, out _, out _));
            Assert.IsFalse(DressingKitMapper.ToSiteCatalog(null).TryGet("camp-kit", out _));
        }
    }
}
