#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Narrative.Data.Definitions;
using Combat.Data.Definitions;

namespace Editor
{
    /// <summary>
    /// Editor utility for setting up narrative system assets.
    /// </summary>
    public static class NarrativeSetupEditor
    {
        private const string NpcAssetPath = "Assets/__Project/Resources/NPCs";
        private const string DialogueSessionAssetPath = "Assets/__Project/Resources/DialogueSessions";
        private const string ChapterAssetPath = "Assets/__Project/Resources/Chapters";
        private const string SideStoryAssetPath = "Assets/__Project/Resources/SideStories";

        [MenuItem("Narrative/Create Sample Assets")]
        public static void CreateSampleAssets()
        {
            EnsureDirectoriesExist();
            CreateSampleNpcs();
            CreateSampleChapter();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NarrativeSetupEditor] Sample assets created successfully!");
        }

        [MenuItem("Narrative/Create Sample Side Stories")]
        public static void CreateSampleSideStories()
        {
            EnsureDirectoriesExist();
            CreateGuildSideStories();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NarrativeSetupEditor] Sample side stories created successfully!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(NpcAssetPath))
                Directory.CreateDirectory(NpcAssetPath);

            if (!Directory.Exists(DialogueSessionAssetPath))
                Directory.CreateDirectory(DialogueSessionAssetPath);

            if (!Directory.Exists(ChapterAssetPath))
                Directory.CreateDirectory(ChapterAssetPath);

            if (!Directory.Exists(SideStoryAssetPath))
                Directory.CreateDirectory(SideStoryAssetPath);
        }

        private static void CreateSampleNpcs()
        {
            // Create Merchant Tomas NPC
            var merchantPath = $"{NpcAssetPath}/NPC_MerchantTomas.asset";
            if (!File.Exists(merchantPath))
            {
                var merchant = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(merchant, "merchant_tomas", "Merchant Tomas",
                    "A friendly traveling merchant who sells wares on the platforms.",
                    NpcFaction.Merchant, "merchant_intro", false);
                AssetDatabase.CreateAsset(merchant, merchantPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {merchantPath}");
            }

            // Create Bandit Leader NPC
            var banditPath = $"{NpcAssetPath}/NPC_BanditLeader.asset";
            if (!File.Exists(banditPath))
            {
                var bandit = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(bandit, "bandit_leader", "Bandit",
                    "A rough bandit who demands tolls from travelers.",
                    NpcFaction.Hostile, "bandit_encounter", true);
                AssetDatabase.CreateAsset(bandit, banditPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {banditPath}");
            }
        }

        private static void SetNpcValues(NpcDefinition npc, string id, string displayName,
            string description, NpcFaction faction, string dialogueKnot, bool canBecomeEnemy)
        {
            // Use reflection to set private serialized fields
            var type = typeof(NpcDefinition);

            SetPrivateField(type, npc, "_npcId", id);
            SetPrivateField(type, npc, "_displayName", displayName);
            SetPrivateField(type, npc, "_description", description);
            SetPrivateField(type, npc, "_faction", faction);
            SetPrivateField(type, npc, "_defaultDialogueKnot", dialogueKnot);
            SetPrivateField(type, npc, "_canBecomeEnemy", canBecomeEnemy);
        }

        private static void CreateSampleChapter()
        {
            var chapterPath = $"{ChapterAssetPath}/Chapter1_Awakening.asset";
            if (!File.Exists(chapterPath))
            {
                var chapter = ScriptableObject.CreateInstance<StoryChapterDefinition>();
                SetChapterValues(chapter, "chapter_1", "The Awakening",
                    "You awaken on a mysterious floating platform, beginning your journey.",
                    1, LevelGeneration.LevelTheme.Forest, "start");
                AssetDatabase.CreateAsset(chapter, chapterPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {chapterPath}");
            }
        }

        private static void SetChapterValues(StoryChapterDefinition chapter, string id, string displayName,
            string description, int chapterNumber, LevelGeneration.LevelTheme theme, string startingKnot)
        {
            var type = typeof(StoryChapterDefinition);

            SetPrivateField(type, chapter, "_chapterId", id);
            SetPrivateField(type, chapter, "_displayName", displayName);
            SetPrivateField(type, chapter, "_description", description);
            SetPrivateField(type, chapter, "_chapterNumber", chapterNumber);
            SetPrivateField(type, chapter, "_theme", theme);
            SetPrivateField(type, chapter, "_startingKnot", startingKnot);
            SetPrivateField(type, chapter, "_baseDifficulty", 1);
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

        private static void CreateGuildSideStories()
        {
            // Create Guild NPCs first
            CreateGuildNpcs();

            // Create Guild Recruiter Side Story
            var recruiterPath = $"{SideStoryAssetPath}/GuildRecruiterStory.asset";
            if (!File.Exists(recruiterPath))
            {
                var sideStory = ScriptableObject.CreateInstance<SideStoryDefinition>();
                SetSideStoryValues(sideStory,
                    "guild_recruiter",
                    "The Ironclad Guild",
                    "A guild recruiter seeks worthy members to join their ancient order.",
                    "guild_recruiter",
                    Narrative.StoryPlatformType.Dialogue,
                    60,
                    new[] { "guild", "faction" });
                AssetDatabase.CreateAsset(sideStory, recruiterPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {recruiterPath}");
            }

            // Create Guild Mission Side Story
            var missionPath = $"{SideStoryAssetPath}/GuildMissionStory.asset";
            if (!File.Exists(missionPath))
            {
                var sideStory = ScriptableObject.CreateInstance<SideStoryDefinition>();
                SetSideStoryValuesWithPrereqs(sideStory,
                    "guild_mission",
                    "Guild Mission: Bandit Hunt",
                    "The guild captain has a mission for you: deal with the bandits raiding supply routes.",
                    "guild_mission_giver",
                    Narrative.StoryPlatformType.Dialogue,
                    70,
                    new[] { "guild", "combat", "quest" },
                    new[] { "guild_initiation" },
                    "joined_guild");
                AssetDatabase.CreateAsset(sideStory, missionPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {missionPath}");
            }
        }

        private static void CreateGuildNpcs()
        {
            // Create Guild Recruiter NPC
            var recruiterPath = $"{NpcAssetPath}/NPC_GuildRecruiter.asset";
            if (!File.Exists(recruiterPath))
            {
                var npc = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(npc, "guild_recruiter", "Guild Recruiter",
                    "A well-armored member of the Ironclad Guild seeking new recruits.",
                    NpcFaction.Neutral, "guild_recruiter", false);
                AssetDatabase.CreateAsset(npc, recruiterPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {recruiterPath}");
            }

            // Create Guild Captain NPC
            var captainPath = $"{NpcAssetPath}/NPC_GuildCaptain.asset";
            if (!File.Exists(captainPath))
            {
                var npc = ScriptableObject.CreateInstance<NpcDefinition>();
                SetNpcValues(npc, "guild_captain", "Guild Captain",
                    "A seasoned veteran who oversees guild operations and assigns missions.",
                    NpcFaction.Friendly, "guild_mission_giver", false);
                AssetDatabase.CreateAsset(npc, captainPath);
                Debug.Log($"[NarrativeSetupEditor] Created: {captainPath}");
            }
        }

        private static void SetSideStoryValues(SideStoryDefinition sideStory, string id, string displayName,
            string description, string startingKnot, Narrative.StoryPlatformType platformType,
            int basePriority, string[] tags)
        {
            var type = typeof(SideStoryDefinition);

            SetPrivateField(type, sideStory, "_storyId", id);
            SetPrivateField(type, sideStory, "_displayName", displayName);
            SetPrivateField(type, sideStory, "_description", description);
            SetPrivateField(type, sideStory, "_startingKnot", startingKnot);
            SetPrivateField(type, sideStory, "_platformType", platformType);
            SetPrivateField(type, sideStory, "_basePriority", basePriority);

            var tagsList = new System.Collections.Generic.List<string>(tags);
            SetPrivateField(type, sideStory, "_tags", tagsList);
        }

        private static void SetSideStoryValuesWithPrereqs(SideStoryDefinition sideStory, string id, string displayName,
            string description, string startingKnot, Narrative.StoryPlatformType platformType,
            int basePriority, string[] tags, string[] requiredCompletedQuests, string customVariable)
        {
            SetSideStoryValues(sideStory, id, displayName, description, startingKnot, platformType, basePriority, tags);

            // Create and set prerequisites
            var prereqs = new StoryPrerequisites();
            var prereqsType = typeof(StoryPrerequisites);

            // Set required completed quests
            var questsList = new System.Collections.Generic.List<string>(requiredCompletedQuests);
            SetPrivateField(prereqsType, prereqs, "_requiredCompletedQuests", questsList);

            // Set custom variable condition if provided
            if (!string.IsNullOrEmpty(customVariable))
            {
                var condition = new VariableCondition();
                var conditionType = typeof(VariableCondition);
                SetPrivateField(conditionType, condition, "_variableName", customVariable);
                SetPrivateField(conditionType, condition, "_comparison", ComparisonOperator.IsTrue);

                var conditionsList = new System.Collections.Generic.List<VariableCondition> { condition };
                SetPrivateField(prereqsType, prereqs, "_customVariableConditions", conditionsList);
            }

            SetPrivateField(typeof(SideStoryDefinition), sideStory, "_prerequisites", prereqs);
        }

        [MenuItem("Narrative/Compile Ink Stories")]
        public static void CompileInkStories()
        {
            Debug.Log("[NarrativeSetupEditor] To compile Ink stories, use the Ink Unity Integration plugin.");
            Debug.Log("[NarrativeSetupEditor] Right-click on .ink files and select 'Recompile Ink' or use Window > Ink > All Compiler Options");
        }
    }
}
#endif
