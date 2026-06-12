using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Inventory.Data.Definitions;
using LevelGeneration;
using Loot.Application;
using Loot.Core;
using Narrative.Data.Definitions;
using Narrative.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class QuestRewardGranterTests
    {
        private sealed class SilentLogger : IGameLogger
        {
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
        }

        private sealed class StubCatalog : IBiomeLootCatalog
        {
            private readonly BiomeLootData _biome;

            public StubCatalog(BiomeLootData biome)
            {
                _biome = biome;
            }

            public BiomeLootData Get(LevelTheme theme)
            {
                return _biome != null && _biome.Theme == theme ? _biome : null;
            }
        }

        private InventoryModel _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
        }

        private static ArtifactDefinition CreateArtifact(string id)
        {
            var artifact = ScriptableObject.CreateInstance<ArtifactDefinition>();
            var so = new UnityEditor.SerializedObject(artifact);
            so.FindProperty("_id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return artifact;
        }

        private static RewardDefinition CreateReward(string rewardId, RewardType type, string itemId)
        {
            var reward = ScriptableObject.CreateInstance<RewardDefinition>();
            var so = new UnityEditor.SerializedObject(reward);
            so.FindProperty("_rewardId").stringValue = rewardId;
            so.FindProperty("_rewardType").enumValueIndex = (int)type;
            so.FindProperty("_itemId").stringValue = itemId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return reward;
        }

        private static StoryDefinition CreateStory(string id)
        {
            var story = ScriptableObject.CreateInstance<StoryDefinition>();
            var so = new UnityEditor.SerializedObject(story);
            so.FindProperty("_storyId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return story;
        }

        private static NpcDefinition CreateNpc(string id)
        {
            var npc = ScriptableObject.CreateInstance<NpcDefinition>();
            var so = new UnityEditor.SerializedObject(npc);
            so.FindProperty("_npcId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        private QuestRewardGranter CreateGranter(
            IReadOnlyList<LootEntryData> questTable = null,
            params string[] knownArtifactIds)
        {
            var biome = new BiomeLootData(
                LevelTheme.Forest,
                platformLootChance: 0f,
                platformLootCountMin: 0,
                platformLootCountMax: 0,
                platformTable: null,
                enemyDropChance: 0f,
                enemyDropTable: null,
                questRewardCountMin: questTable != null ? 1 : 0,
                questRewardCountMax: questTable != null ? 1 : 0,
                questTable: questTable);

            var seedProvider = new RunSeedProvider();
            seedProvider.SetSeed(1234);
            var lootRollService = new LootRollService(
                new StubCatalog(biome), seedProvider, null, tagBiasMultiplier: 2f);

            var themeProvider = new CurrentThemeProvider();
            themeProvider.SetTheme(LevelTheme.Forest);

            var artifacts = new List<ArtifactDefinition>();
            foreach (var id in knownArtifactIds)
            {
                artifacts.Add(CreateArtifact(id));
            }

            return new QuestRewardGranter(
                lootRollService,
                themeProvider,
                _inventory,
                new ArtifactCatalog(artifacts),
                new SilentLogger());
        }

        private static NpcAssignment CreateAssignment(params ResolvedReward[] rewards)
        {
            return new NpcAssignment(CreateNpc("npc1"), CreateStory("story1"), rewards);
        }

        [Test]
        public void GrantForAssignment_ItemReward_AddsArtifactsToInventory()
        {
            var granter = CreateGranter(questTable: null, "fire");
            var reward = new ResolvedReward(CreateReward("r1", RewardType.Item, "fire"), 3);

            granter.GrantForAssignment(CreateAssignment(reward));

            Assert.AreEqual(3, _inventory.Items.Count);
            Assert.AreEqual("fire", _inventory.Items[0].DefinitionId);
        }

        [Test]
        public void GrantForAssignment_NonItemReward_IsSkipped()
        {
            var granter = CreateGranter(questTable: null, "fire");
            var reward = new ResolvedReward(CreateReward("r1", RewardType.Currency, "fire"), 5);

            granter.GrantForAssignment(CreateAssignment(reward));

            Assert.AreEqual(0, _inventory.Items.Count);
        }

        [Test]
        public void GrantForAssignment_UnknownArtifactId_IsSkipped()
        {
            var granter = CreateGranter(questTable: null, "fire");
            var reward = new ResolvedReward(CreateReward("r1", RewardType.Item, "not-an-artifact"), 1);

            granter.GrantForAssignment(CreateAssignment(reward));

            Assert.AreEqual(0, _inventory.Items.Count);
        }

        [Test]
        public void GrantForAssignment_ProceduralReward_RolledFromBiomeQuestTable()
        {
            var questTable = new[] { new LootEntryData("water", 1f) };
            var granter = CreateGranter(questTable, "water");

            granter.GrantForAssignment(CreateAssignment());

            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreEqual("water", _inventory.Items[0].DefinitionId);
        }

        [Test]
        public void GrantForAssignment_SameContext_IsDeterministic()
        {
            var questTable = new[]
            {
                new LootEntryData("fire", 1f),
                new LootEntryData("water", 1f),
                new LootEntryData("rock", 1f)
            };

            var granter = CreateGranter(questTable, "fire", "water", "rock");
            granter.GrantForAssignment(CreateAssignment());
            var firstRun = _inventory.Items[0].DefinitionId;

            _inventory = new InventoryModel();
            var secondGranter = CreateGranter(questTable, "fire", "water", "rock");
            secondGranter.GrantForAssignment(CreateAssignment());

            Assert.AreEqual(firstRun, _inventory.Items[0].DefinitionId);
        }
    }
}
