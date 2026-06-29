using Narrative.Interaction.Core;

namespace Narrative.Interaction.Data
{
    /// <summary>
    /// The single bridge from the authored <see cref="NpcInteractionConfig"/> SO into the pure-C#
    /// <see cref="NpcInteractionSettings"/> used by the proximity logic. Falls back to sensible defaults
    /// when no config asset is wired, so the system runs without per-scene authoring.
    /// </summary>
    public static class NpcInteractionConfigMapper
    {
        private const float DefaultInteractionRadius = 3.5f;
        private const float DefaultAggroRadius = 2.5f;

        public static NpcInteractionSettings ToSettings(NpcInteractionConfig config)
        {
            if (config == null)
            {
                return new NpcInteractionSettings(DefaultInteractionRadius, DefaultAggroRadius);
            }

            return new NpcInteractionSettings(config.InteractionRadius, config.AggroRadius);
        }
    }
}
