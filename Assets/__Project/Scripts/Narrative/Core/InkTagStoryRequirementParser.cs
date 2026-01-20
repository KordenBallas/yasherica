using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// Parses story requirements from Ink tags.
    ///
    /// Supported tags:
    /// - platform_type: simple|combat|dialogue|cutscene
    /// - npc: <npc_id>
    /// - npc_can_become_enemy: true|false
    /// - enemy_id: <id>
    /// - outcome: Continue|Combat|Quest|Trade
    /// - key_node: true|false
    /// - priority: <number>
    /// - require_quest: <quest_id>
    /// - require_npc_met: <npc_id>
    /// </summary>
    public class InkTagStoryRequirementParser : IStoryRequirementParser
    {
        private readonly IStoryManager _storyManager;

        private const string TagPlatformType = "platform_type";
        private const string TagNpc = "npc";
        private const string TagNpcCanBecomeEnemy = "npc_can_become_enemy";
        private const string TagEnemyId = "enemy_id";
        private const string TagOutcome = "outcome";
        private const string TagKeyNode = "key_node";
        private const string TagPriority = "priority";
        private const string TagRequireQuest = "require_quest";
        private const string TagRequireNpcMet = "require_npc_met";
        private const string TagSpeaker = "speaker";

        public InkTagStoryRequirementParser(IStoryManager storyManager)
        {
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
        }

        public IReadOnlyList<StoryNodeRequirement> ParseRequirements(StoryChapterDefinition chapter)
        {
            var requirements = new List<StoryNodeRequirement>();

            if (chapter == null || !chapter.HasInkContent)
            {
                return requirements;
            }

            // Parse requirements from key dialogue sessions
            foreach (var session in chapter.KeyDialogueSessions)
            {
                if (session?.Npc == null)
                    continue;

                var requirement = CreateRequirementFromSession(session);
                requirements.Add(requirement);
            }

            // Parse requirements from required NPCs
            int priority = 0;
            foreach (var npc in chapter.RequiredNpcs)
            {
                if (npc == null)
                    continue;

                // Skip if already added via dialogue session
                if (requirements.Exists(r => r.NpcId == npc.NpcId))
                    continue;

                var requirement = CreateRequirementFromNpc(npc, priority++);
                requirements.Add(requirement);
            }

            return requirements;
        }

        public StoryNodeRequirement ParseFromTags(string knotName, IReadOnlyList<string> tags)
        {
            var requirement = new StoryNodeRequirement
            {
                NodeId = knotName,
                InkPath = knotName
            };

            if (tags == null)
                return requirement;

            foreach (var tag in tags)
            {
                ParseTag(tag, requirement);
            }

            return requirement;
        }

        public void ParseTag(string tag, StoryNodeRequirement requirement)
        {
            if (string.IsNullOrEmpty(tag) || requirement == null)
                return;

            // Parse tag format: "key: value" or "key:value"
            var colonIndex = tag.IndexOf(':');
            if (colonIndex <= 0)
            {
                requirement.Tags.Add(tag.Trim());
                return;
            }

            var key = tag.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = tag.Substring(colonIndex + 1).Trim();

            switch (key)
            {
                case TagPlatformType:
                    requirement.PlatformType = ParsePlatformType(value);
                    break;

                case TagNpc:
                    requirement.NpcId = value;
                    break;

                case TagNpcCanBecomeEnemy:
                    requirement.NpcCanBecomeEnemy = ParseBool(value);
                    break;

                case TagEnemyId:
                    requirement.EnemyId = value;
                    break;

                case TagOutcome:
                    requirement.ExpectedOutcome = ParseOutcome(value);
                    break;

                case TagKeyNode:
                    requirement.IsKeyNode = ParseBool(value);
                    break;

                case TagPriority:
                    if (int.TryParse(value, out var priority))
                    {
                        requirement.Priority = priority;
                    }
                    break;

                case TagRequireQuest:
                    requirement.RequiredActiveQuests.Add(value);
                    break;

                case TagRequireNpcMet:
                    requirement.RequiredEncounteredNpcs.Add(value);
                    break;

                case TagSpeaker:
                    // Speaker tag is for dialogue display, not requirement parsing
                    requirement.Tags.Add(tag);
                    break;

                default:
                    // Store unknown tags for potential custom processing
                    requirement.Tags.Add(tag);
                    break;
            }
        }

        private StoryNodeRequirement CreateRequirementFromSession(DialogueSessionDefinition session)
        {
            var requirement = new StoryNodeRequirement
            {
                NodeId = session.SessionId,
                InkPath = session.FullInkPath,
                NpcId = session.Npc.NpcId,
                NpcCanBecomeEnemy = session.Npc.CanBecomeEnemy,
                IsKeyNode = true,
                PlatformType = StoryPlatformType.Dialogue
            };

            if (session.Npc.EnemyDefinition != null)
            {
                requirement.EnemyId = session.Npc.EnemyDefinition.EnemyId.ToString();
            }

            foreach (var questId in session.RequiredActiveQuests)
            {
                requirement.RequiredActiveQuests.Add(questId);
            }

            foreach (var npcId in session.RequiredEncounteredNpcs)
            {
                requirement.RequiredEncounteredNpcs.Add(npcId);
            }

            // Determine expected outcome from possible outcomes
            if (session.PossibleOutcomes.Count > 0)
            {
                // Use the first possible outcome as the expected one
                requirement.ExpectedOutcome = session.PossibleOutcomes[0].outcomeType;
            }

            return requirement;
        }

        private StoryNodeRequirement CreateRequirementFromNpc(NpcDefinition npc, int priority)
        {
            var requirement = new StoryNodeRequirement
            {
                NodeId = $"npc_{npc.NpcId}",
                InkPath = npc.DefaultDialogueKnot,
                NpcId = npc.NpcId,
                NpcCanBecomeEnemy = npc.CanBecomeEnemy,
                Priority = priority,
                PlatformType = StoryPlatformType.Dialogue
            };

            if (npc.EnemyDefinition != null)
            {
                requirement.EnemyId = npc.EnemyDefinition.EnemyId.ToString();
            }

            // Set key node based on faction
            requirement.IsKeyNode = npc.Faction == NpcFaction.QuestGiver;

            return requirement;
        }

        private StoryPlatformType ParsePlatformType(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "simple" => StoryPlatformType.Simple,
                "combat" => StoryPlatformType.Combat,
                "dialogue" => StoryPlatformType.Dialogue,
                "cutscene" => StoryPlatformType.Cutscene,
                _ => StoryPlatformType.Simple
            };
        }

        private DialogueOutcomeType ParseOutcome(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "continue" => DialogueOutcomeType.Continue,
                "combat" => DialogueOutcomeType.Combat,
                "quest" => DialogueOutcomeType.Quest,
                "trade" => DialogueOutcomeType.Trade,
                "exit" => DialogueOutcomeType.Exit,
                _ => DialogueOutcomeType.Continue
            };
        }

        private bool ParseBool(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "true" or "1" or "yes" => true,
                _ => false
            };
        }
    }
}
