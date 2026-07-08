using System.Collections.Generic;
using System.Reflection;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Combat.Integration;
using Core.Logging;
using Core.Persistence;
using Hub.Data;
using Inventory.Data;
using Inventory.Data.Definitions;
using Loot.Core;
using Loot.Data;
using MetaProgression.Core;
using MetaProgression.Data;
using MetaProgression.Integration;
using Mutation.Core;
using Mutation.Data;
using Narrative.Facts.Core;
using Narrative.Facts.Data;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Per-surface meta-gating coverage (Track R, FR3): a gated token whose deed is unmet is
    /// absent from the hub dig pool, the world loot tables, the quest-reward pools, the recipe
    /// book, and the mutation candidates — and appears once the deed fact lands (the run-N →
    /// run-N+1 loop at each surface). Unmarked content always passes (FR15).
    /// </summary>
    [TestFixture]
    public class MetaGatingSurfaceTests
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

        // A meta-gated block whose deed is "this token has been tasted" ($self binding).
        private static MetaGatingAuthoring TasteGatedBlock(int tier = 0)
        {
            var block = new MetaGatingAuthoring();
            SetPrivate(block, "_mark", GatingMark.MetaGated);
            SetPrivate(block, "_deed", new List<FactPredicateSerial>
            {
                new FactPredicateSerial(FactNamespace.World, "$self", "arena_tasted", ComparisonOp.Eq,
                    new FactValueAuthoring(FactValueType.Bool, boolValue: true))
            });
            SetPrivate(block, "_unlockTier", tier);
            return block;
        }

        private static FactStoreSnapshot SnapshotWithTasted(params string[] partIds)
        {
            var snapshot = new FactStoreSnapshot();
            foreach (var partId in partIds)
            {
                snapshot.Entries.Add(new FactEntryDto
                {
                    Namespace = FactNamespace.World,
                    Subject = partId,
                    Key = "arena_tasted",
                    Type = FactValueType.Bool,
                    BoolValue = true
                });
            }

            return snapshot;
        }

        private static IMetaVocabulary Vocabulary(FactStoreSnapshot snapshot) =>
            new MetaVocabulary(snapshot, MetaProgressionSettings.Defaults, effectiveRunCount: 1);

        private SlotDefinition NewSlot(string id)
        {
            var slot = ScriptableObject.CreateInstance<SlotDefinition>();
            _created.Add(slot);
            SetPrivate(slot, "_id", id);
            return slot;
        }

        private PartDefinition NewPart(string id, MetaGatingAuthoring gating = null)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            part.name = id;
            _created.Add(part);
            SetPrivate(part, "_id", id);
            SetPrivate(part, "_slot", NewSlot("slot_" + id));
            if (gating != null)
            {
                SetPrivate(part, "_metaGating", gating);
            }

            return part;
        }

        private ArtifactDefinition NewArtifact(string id, MetaGatingAuthoring gating = null)
        {
            var artifact = ScriptableObject.CreateInstance<ArtifactDefinition>();
            artifact.name = id;
            _created.Add(artifact);
            SetPrivate(artifact, "_id", id);
            if (gating != null)
            {
                SetPrivate(artifact, "_metaGating", gating);
            }

            return artifact;
        }

        private RecipeDefinition NewRecipe(ArtifactDefinition input, ArtifactDefinition output,
            MetaGatingAuthoring gating = null)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.name = "recipe_" + output.Id;
            _created.Add(recipe);
            SetPrivate(recipe, "_inputs", new List<ArtifactDefinition> { input });
            SetPrivate(recipe, "_output", output);
            if (gating != null)
            {
                SetPrivate(recipe, "_metaGating", gating);
            }

            return recipe;
        }

        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeMetaStore : IMetaMemoryStore
        {
            private readonly MetaMemorySnapshot _snapshot;

            public FakeMetaStore(FactStoreSnapshot facts)
            {
                _snapshot = new MetaMemorySnapshot { Facts = facts ?? new FactStoreSnapshot() };
            }

            public MetaMemorySnapshot LoadOrEmpty() => _snapshot;

            public void Save(MetaMemorySnapshot snapshot) { }
        }

        // --- Mutation candidates ---

        [Test]
        public void MutationCatalog_ExcludesLockedPart_IncludesAfterDeed()
        {
            var gated = NewPart("part_gated", TasteGatedBlock());
            var open = NewPart("part_open");
            var partCatalog = new PartCatalog(new[] { gated, open });
            var resolver = new PartAbilityResolver(partCatalog, null);

            var locked = new MutationPartCatalog(partCatalog, resolver, Vocabulary(SnapshotWithTasted()));
            var unlocked = new MutationPartCatalog(partCatalog, resolver, Vocabulary(SnapshotWithTasted("part_gated")));

            Assert.AreEqual(new[] { "part_open" }, CandidateIds(locked));
            CollectionAssert.AreEquivalent(new[] { "part_open", "part_gated" }, CandidateIds(unlocked));
            Assert.IsFalse(locked.TryGetCardData("part_gated", out _));
            Assert.IsTrue(unlocked.TryGetCardData("part_gated", out _));
        }

        [Test]
        public void MutationCatalog_NullVocabulary_PassesEverything()
        {
            var gated = NewPart("part_gated", TasteGatedBlock());
            var partCatalog = new PartCatalog(new[] { gated });

            var catalog = new MutationPartCatalog(partCatalog, new PartAbilityResolver(partCatalog, null));

            Assert.AreEqual(new[] { "part_gated" }, CandidateIds(catalog));
        }

        private static List<string> CandidateIds(MutationPartCatalog catalog)
        {
            var ids = new List<string>();
            foreach (var candidate in catalog.AllCandidates)
            {
                ids.Add(candidate.PartId);
            }

            return ids;
        }

        // --- Recipe book ---

        [Test]
        public void RecipeBook_SkipsLockedRecipe_ContainsItAfterDeed()
        {
            var input = NewArtifact("fire");
            var output = NewArtifact("snake");
            // The recipe's token id is its output artifact id; deed = taste the serpent part.
            var gating = new MetaGatingAuthoring();
            SetPrivate(gating, "_mark", GatingMark.MetaGated);
            SetPrivate(gating, "_deed", new List<FactPredicateSerial>
            {
                new FactPredicateSerial(FactNamespace.World, "part_serpent", "arena_tasted", ComparisonOp.Eq,
                    new FactValueAuthoring(FactValueType.Bool, boolValue: true))
            });
            var recipe = NewRecipe(input, output, gating);
            var definitions = new List<RecipeDefinition> { recipe };
            var inputs = new List<string> { "fire" };

            var lockedBook = new RecipeBookBuilder(new FakeLogger(), Vocabulary(SnapshotWithTasted()))
                .Build(definitions);
            var unlockedBook = new RecipeBookBuilder(new FakeLogger(), Vocabulary(SnapshotWithTasted("part_serpent")))
                .Build(definitions);

            Assert.IsFalse(lockedBook.TryMatch(inputs, out _));
            Assert.IsTrue(unlockedBook.TryMatch(inputs, out var outputId) && outputId == "snake");
        }

        // --- Quest reward pools ---

        [Test]
        public void QuestRewardPools_FilterLockedArtifactAndBlank()
        {
            var artifacts = new ArtifactCatalog(new[]
            {
                NewArtifact("art_open"),
                NewArtifact("art_gated", TasteGatedBlock())
            });
            var blanks = new FakePartBlankDataSource()
                .Add(new PartBlankData("blank_open", "Open", "slot", "species", 2))
                .Add(new PartBlankData("blank_gated", "Gated", "slot", "species", 2,
                    gate: new MetaGate(GatingMark.MetaGated, null, 5, 1f)));

            var pools = QuestRewardPoolsBuilder.Build(artifacts, blanks, Vocabulary(SnapshotWithTasted()));

            Assert.AreEqual(1, pools.Artifacts.Count);
            Assert.AreEqual("art_open", pools.Artifacts[0].Id);
            Assert.AreEqual(1, pools.Blanks.Count);
            Assert.AreEqual("blank_open", pools.Blanks[0].Id);
        }

        [Test]
        public void QuestRewardPools_NullVocabulary_PassesAll()
        {
            var artifacts = new ArtifactCatalog(new[] { NewArtifact("art_gated", TasteGatedBlock()) });
            var blanks = new FakePartBlankDataSource();

            var pools = QuestRewardPoolsBuilder.Build(artifacts, blanks, null);

            Assert.AreEqual(1, pools.Artifacts.Count);
        }

        // --- World loot filter ---

        [Test]
        public void LootFilter_BlocksLocked_PassesUnlockedAndUnknown()
        {
            var artifacts = new ArtifactCatalog(new[]
            {
                NewArtifact("art_open"),
                NewArtifact("art_gated", TasteGatedBlock())
            });
            var filter = new MetaGateLootFilter(artifacts, Vocabulary(SnapshotWithTasted()));
            var context = new LootRollContext(default, "test");

            Assert.IsTrue(filter.IsEligible(new LootEntryData("art_open", 1f), context));
            Assert.IsFalse(filter.IsEligible(new LootEntryData("art_gated", 1f), context));
            Assert.IsTrue(filter.IsEligible(new LootEntryData("art_unknown", 1f), context));
        }

        [Test]
        public void LootFilter_NullVocabulary_FiltersNothing()
        {
            var artifacts = new ArtifactCatalog(new[] { NewArtifact("art_gated", TasteGatedBlock()) });
            var filter = new MetaGateLootFilter(artifacts, null);

            Assert.IsTrue(filter.IsEligible(new LootEntryData("art_gated", 1f), new LootRollContext(default, "test")));
        }

        // --- Hub dig pool (Tasted ∩ Eligible) ---

        [Test]
        public void HubPool_IsTastedIntersectEligible()
        {
            var tastedOpen = NewPart("part_tasted_open");
            var tastedGated = NewPart("part_tasted_gated", new MetaGatingAuthoring());
            SetPrivate(tastedGated.MetaGating, "_mark", GatingMark.MetaGated);
            SetPrivate(tastedGated.MetaGating, "_deed", new List<FactPredicateSerial>
            {
                new FactPredicateSerial(FactNamespace.World, string.Empty, "run_count", ComparisonOp.Gte,
                    new FactValueAuthoring(FactValueType.Int, intValue: 5))
            });
            var untasted = NewPart("part_untasted");
            var partCatalog = new PartCatalog(new[] { tastedOpen, tastedGated, untasted });

            var tastedFacts = SnapshotWithTasted("part_tasted_open", "part_tasted_gated");
            var reader = new Combat.Arena.Data.ArenaTastedCatalogReader(
                new FakeMetaStore(tastedFacts), partCatalog);
            var pool = new HubStartingPoolSource(reader, partCatalog,
                new MetaVocabulary(tastedFacts, MetaProgressionSettings.Defaults, effectiveRunCount: 1));

            var ids = new List<string>();
            foreach (var candidate in pool.BuildPool())
            {
                ids.Add(candidate.PartId);
            }

            // Untasted is out (tasted rule), the run-5-gated form is withheld (eligibility rule).
            Assert.AreEqual(new[] { "part_tasted_open" }, ids);
        }
    }
}
