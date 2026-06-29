namespace Narrative.Interaction.Core
{
    /// <summary>
    /// The two global distances that drive NPC proximity (R13): the interaction radius (F-prompt range for
    /// talkable NPCs) and the aggro radius (auto-battle range for hostile NPCs). Plain C#, mapped from the
    /// authored <c>NpcInteractionConfig</c> SO at install time. Per-NPC overrides are out of scope.
    /// </summary>
    public sealed class NpcInteractionSettings
    {
        public float InteractionRadius { get; }
        public float AggroRadius { get; }

        public NpcInteractionSettings(float interactionRadius, float aggroRadius)
        {
            InteractionRadius = interactionRadius;
            AggroRadius = aggroRadius;
        }
    }
}
