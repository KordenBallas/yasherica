using System;
using System.Collections.Generic;

namespace Narrative
{
    /// <summary>
    /// Maps story requirements to game content (platforms, NPCs, etc.).
    /// Parsed from Ink tags to drive scenario generation.
    /// </summary>
    [Serializable]
    public class StoryNodeRequirement
    {
        /// <summary>
        /// Unique identifier for this story node.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The Ink knot/stitch path for this node.
        /// </summary>
        public string InkPath { get; set; }

        /// <summary>
        /// Type of platform required for this story node.
        /// </summary>
        public StoryPlatformType PlatformType { get; set; }

        /// <summary>
        /// NPC ID if this node requires an NPC.
        /// </summary>
        public string NpcId { get; set; }

        /// <summary>
        /// Whether the NPC can transition to an enemy.
        /// </summary>
        public bool NpcCanBecomeEnemy { get; set; }

        /// <summary>
        /// Enemy ID if NPC transitions to combat.
        /// </summary>
        public string EnemyId { get; set; }

        /// <summary>
        /// Expected outcome type of this story node.
        /// </summary>
        public DialogueOutcomeType ExpectedOutcome { get; set; }

        /// <summary>
        /// Whether this is a key story node (required for progression).
        /// </summary>
        public bool IsKeyNode { get; set; }

        /// <summary>
        /// Order priority for placement (lower = earlier).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Additional metadata tags.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Required quest IDs to be active for this node to appear.
        /// </summary>
        public List<string> RequiredActiveQuests { get; set; } = new();

        /// <summary>
        /// Required NPCs to have been encountered for this node to appear.
        /// </summary>
        public List<string> RequiredEncounteredNpcs { get; set; } = new();

        public StoryNodeRequirement()
        {
            PlatformType = StoryPlatformType.Simple;
            ExpectedOutcome = DialogueOutcomeType.Continue;
        }

        public StoryNodeRequirement(string nodeId, string inkPath)
        {
            NodeId = nodeId;
            InkPath = inkPath;
            PlatformType = StoryPlatformType.Simple;
            ExpectedOutcome = DialogueOutcomeType.Continue;
        }

        /// <summary>
        /// Checks if this requirement has an associated NPC.
        /// </summary>
        public bool HasNpc => !string.IsNullOrEmpty(NpcId);

        /// <summary>
        /// Checks if this is a combat-oriented node.
        /// </summary>
        public bool IsCombatNode => PlatformType == StoryPlatformType.Combat || ExpectedOutcome == DialogueOutcomeType.Combat;
    }

    /// <summary>
    /// Type of platform for story content.
    /// </summary>
    public enum StoryPlatformType
    {
        Simple,
        Combat,
        Dialogue,
        Cutscene
    }

    /// <summary>
    /// Possible outcomes of a dialogue interaction.
    /// </summary>
    public enum DialogueOutcomeType
    {
        Continue,   // Dialogue ended normally
        Combat,     // Transitioned to combat
        Quest,      // Quest started or updated
        Trade,      // Trading interface opened
        Exit        // Player exited dialogue early
    }
}
