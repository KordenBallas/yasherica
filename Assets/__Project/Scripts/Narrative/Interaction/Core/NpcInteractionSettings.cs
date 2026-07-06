namespace Narrative.Interaction.Core
{
    /// <summary>
    /// The global distances that drive NPC proximity (R13): the interaction radius (F-prompt range for
    /// talkable NPCs), the aggro radius (auto-battle range for hostile NPCs), and the camp boss's larger
    /// engagement radius (both intents; the only per-role radius — a general per-NPC surface stays out
    /// of scope). Plain C#, mapped from the authored <c>NpcInteractionConfig</c> SO at install time.
    /// </summary>
    public sealed class NpcInteractionSettings
    {
        public float InteractionRadius { get; }
        public float AggroRadius { get; }
        public float BossEngagementRadius { get; }

        public NpcInteractionSettings(float interactionRadius, float aggroRadius, float bossEngagementRadius)
        {
            InteractionRadius = interactionRadius;
            AggroRadius = aggroRadius;
            BossEngagementRadius = bossEngagementRadius;
        }
    }
}
