#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Narrative.Data.Definitions;

namespace Yasherica.Editor
{
    /// <summary>
    /// Editor utility for setting up narrative system assets.
    /// </summary>
    public static class NarrativeSetupEditor
    {
        private const string NpcAssetPath = "Assets/__Project/Resources/NPCs";
        private const string StoryAssetPath = "Assets/__Project/Resources/Stories";
        private const string RewardAssetPath = "Assets/__Project/Resources/Rewards";

        [MenuItem("Narrative/Create Sample Assets")]
        public static void CreateSampleAssets()
        {
            EnsureDirectoriesExist();
            CreateSampleNpcs();
            CreateSampleStories();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NarrativeSetupEditor] Sample assets created successfully!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(NpcAssetPath))
                Directory.CreateDirectory(NpcAssetPath);
            if (!Directory.Exists(StoryAssetPath))
                Directory.CreateDirectory(StoryAssetPath);
            if (!Directory.Exists(RewardAssetPath))
                Directory.CreateDirectory(RewardAssetPath);
        }

        private static void CreateSampleNpcs()
        {
            var merchantPath = $"{NpcAssetPath}/NPC_MerchantTomas.asset";
            if (!File.Exists(merchantPath))
            {
                var merchant = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(merchant, "merchant_tomas", "Merchant Tomas",
                    NpcFaction.Merchant, false);
                AssetDatabase.CreateAsset(merchant, merchantPath);
            }

            var banditPath = $"{NpcAssetPath}/NPC_BanditLeader.asset";
            if (!File.Exists(banditPath))
            {
                var bandit = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(bandit, "bandit_leader", "Bandit",
                    NpcFaction.Hostile, true);
                AssetDatabase.CreateAsset(bandit, banditPath);
            }
        }

        private static void CreateSampleStories()
        {
            var storyPath = $"{StoryAssetPath}/Story_TavernTrouble.asset";
            if (!File.Exists(storyPath))
            {
                var story = ScriptableObject.CreateInstance<StoryDefinition>();
                SetPrivateField(typeof(StoryDefinition), story, "_storyId", "tavern_trouble");
                SetPrivateField(typeof(StoryDefinition), story, "_displayName", "Tavern Trouble");
                SetPrivateField(typeof(StoryDefinition), story, "_description", "There's trouble brewing in the tavern.");
                SetPrivateField(typeof(StoryDefinition), story, "_startingKnot", "start");
                SetPrivateField(typeof(StoryDefinition), story, "_isRepeatable", true);
                AssetDatabase.CreateAsset(story, storyPath);
            }
        }

        private static void SetNpcValues(NpcDefinition npc, string id, string displayName,
            NpcFaction faction, bool canBecomeEnemy)
        {
            var type = typeof(NpcDefinition);
            SetPrivateField(type, npc, "_npcId", id);
            SetPrivateField(type, npc, "_displayName", displayName);
            SetPrivateField(type, npc, "_faction", faction);
            SetPrivateField(type, npc, "_canBecomeEnemy", canBecomeEnemy);
        }

        private static void SetPrivateField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[NarrativeSetupEditor] Could not find field: {fieldName}");
            }
        }

        [MenuItem("Narrative/Compile Ink Stories")]
        public static void CompileInkStories()
        {
            Debug.Log("[NarrativeSetupEditor] To compile Ink stories, use the Ink Unity Integration plugin.");
            Debug.Log("[NarrativeSetupEditor] Right-click on .ink files and select 'Recompile Ink'");
        }
    }
}
#endif
