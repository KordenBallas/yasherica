using System;
using System.Collections.Generic;
using System.Linq;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Data.Providers;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Scenario generator that creates sequential NPC encounters.
    /// Places NPCs from NarrativeInstaller's list in order, with empty platforms between each.
    /// Pure C# class - no Unity dependencies except Debug (follows MVP pattern).
    /// </summary>
    public class NpcSequenceScenarioGenerator : IScenarioGenerator
    {
        private readonly INpcDataProvider _npcDataProvider;
        private readonly IStoryStateProvider _storyStateProvider;

        public NpcSequenceScenarioGenerator(
            INpcDataProvider npcDataProvider,
            IStoryStateProvider storyStateProvider)
        {
            _npcDataProvider = npcDataProvider ?? throw new ArgumentNullException(nameof(npcDataProvider));
            _storyStateProvider = storyStateProvider ?? throw new ArgumentNullException(nameof(storyStateProvider));
        }

        public ScenarioData GenerateScenario(GameContext context)
        {
            Debug.Log("[NpcSequenceScenarioGenerator] Generating scenario");
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Get all NPCs from provider
            var allNpcs = _npcDataProvider.GetAllNpcs();

            if (allNpcs == null || allNpcs.Count == 0)
            {
                Debug.LogWarning("[NpcSequenceScenarioGenerator] No NPCs available - generating minimal scenario");
                return GenerateMinimalScenario(context);
            }

            return BuildNpcSequenceScenario(context, allNpcs);
        }

        /// <summary>
        /// Generates minimal scenario with only entry and exit platforms when no NPCs are available.
        /// </summary>
        private ScenarioData GenerateMinimalScenario(GameContext context)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = 1,
                Theme = LevelTheme.Forest,
                ChapterId = _storyStateProvider.CurrentChapterId,
                EstimatedPlatformCount = 2
            };

            var requirements = new List<PlatformRequirement>
            {
                CreateEntryPlatform(),
                CreateExitPlatform()
            };

            scenario.RequiredPlatforms = requirements;
            return scenario;
        }

        /// <summary>
        /// Builds scenario with NPC sequence: Entry → NPC → Empty → NPC → Empty → ... → NPC → Exit
        /// </summary>
        private ScenarioData BuildNpcSequenceScenario(GameContext context, IReadOnlyList<NpcDefinition> npcs)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = CalculateDifficulty(context, npcs),
                Theme = SelectTheme(npcs),
                ChapterId = _storyStateProvider.CurrentChapterId
            };

            var requirements = new List<PlatformRequirement>();

            // 1. Entry platform
            requirements.Add(CreateEntryPlatform());

            // 2. NPC sequence with empty platforms between
            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                requirements.Add(CreateNpcPlatform(npc, i));

                // Add empty platform after each NPC except the last one
                if (i < npcs.Count - 1)
                {
                    requirements.Add(CreateEmptyPlatform());
                }
            }

            // 3. Exit platform
            requirements.Add(CreateExitPlatform());

            scenario.RequiredPlatforms = requirements;
            scenario.EstimatedPlatformCount = requirements.Count;

            Debug.Log($"[NpcSequenceScenarioGenerator] Generated scenario: {requirements.Count} platforms " +
                      $"({npcs.Count} NPCs, {npcs.Count - 1} empty spacers)");

            return scenario;
        }

        /// <summary>
        /// Creates a platform requirement for an NPC.
        /// Platform type depends on whether NPC can become enemy.
        /// </summary>
        private PlatformRequirement CreateNpcPlatform(NpcDefinition npc, int priority)
        {
            var platformType = DeterminePlatformType(npc);

            return new PlatformRequirement
            {
                Type = platformType,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.Npc },
                StoryData = new StoryPlatformData
                {
                    NpcId = npc.NpcId,
                    DialogueKnot = npc.DefaultDialogueKnot,
                    NpcCanBecomeEnemy = npc.CanBecomeEnemy,
                    EnemyId = "" + npc.EnemyDefinition?.EnemyId,
                    IsKeyNode = HasKeyStory(npc),
                    Priority = priority,
                    SideStoryId = null
                }
            };
        }

        /// <summary>
        /// Creates an empty platform for pacing between NPCs.
        /// </summary>
        private PlatformRequirement CreateEmptyPlatform()
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        /// <summary>
        /// Creates entry platform (always first).
        /// </summary>
        private PlatformRequirement CreateEntryPlatform()
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        /// <summary>
        /// Creates exit platform (always last).
        /// </summary>
        private PlatformRequirement CreateExitPlatform()
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        /// <summary>
        /// Determines platform type based on NPC combat capabilities.
        /// Combat if NPC can become enemy, otherwise Simple.
        /// </summary>
        private PlatformType DeterminePlatformType(NpcDefinition npc)
        {
            if (npc.CanBecomeEnemy && npc.EnemyDefinition != null)
            {
                return PlatformType.Combat;
            }

            return PlatformType.Simple;
        }

        /// <summary>
        /// Checks if NPC has any key progression stories.
        /// </summary>
        private bool HasKeyStory(NpcDefinition npc)
        {
            // If NPC has story associations, check if any are key stories
            if (npc.HasStoryAssociations)
            {
                // This is a heuristic - we could enhance this by checking actual story definitions
                // For now, assume NPCs with story associations might be key
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates difficulty based on NPC count, hostile NPCs, and character level.
        /// Formula: 1 + (npcCount / 2) + hostileNpcCount + (characterLevel / 10)
        /// Clamped between 1 and 10.
        /// </summary>
        private int CalculateDifficulty(GameContext context, IReadOnlyList<NpcDefinition> npcs)
        {
            int npcCount = npcs.Count;
            int hostileNpcCount = npcs.Count(n => n.CanBecomeEnemy);

            int baseDifficulty = 1 + (npcCount / 2) + hostileNpcCount + (context.CharacterLevel / 10);

            return Math.Max(1, Math.Min(baseDifficulty, 10));
        }

        /// <summary>
        /// Selects theme from first NPC's first story, or falls back to Forest.
        /// </summary>
        private LevelTheme SelectTheme(IReadOnlyList<NpcDefinition> npcs)
        {
            // Try to get theme from first NPC's first story
            if (npcs.Count > 0)
            {
                var firstNpc = npcs[0];
                if (firstNpc.HasStoryAssociations && firstNpc.AssociatedStories.Count > 0)
                {
                    // In a complete implementation, we would fetch the story definition
                    // and get its theme. For now, we'll use a simple heuristic based on faction
                    return GetThemeFromFaction(firstNpc.Faction);
                }
            }

            // Fallback to Forest
            return LevelTheme.Forest;
        }

        /// <summary>
        /// Maps NPC faction to a level theme (simple heuristic).
        /// </summary>
        private LevelTheme GetThemeFromFaction(NpcFaction faction)
        {
            switch (faction)
            {
                case NpcFaction.Hostile:
                    return LevelTheme.Cave;
                case NpcFaction.Merchant:
                    return LevelTheme.Desert;
                case NpcFaction.QuestGiver:
                    return LevelTheme.Mountain;
                case NpcFaction.Friendly:
                case NpcFaction.Neutral:
                default:
                    return LevelTheme.Forest;
            }
        }
    }
}
