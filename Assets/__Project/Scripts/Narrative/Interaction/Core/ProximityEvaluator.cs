using System.Collections.Generic;

namespace Narrative.Interaction.Core
{
    /// <summary>One NPC's proximity-relevant state this frame, fed to the <see cref="ProximityEvaluator"/>.</summary>
    public readonly struct NpcProximitySample
    {
        public readonly string Id;
        public readonly PlanarPoint Position;
        public readonly NpcIntent Intent;

        /// <summary>True once the NPC's encounter has started, so it neither prompts nor re-aggros.</summary>
        public readonly bool Consumed;

        /// <summary>Camp boss: uses the larger engagement radius and auto-engages (talk or fight) on cross.</summary>
        public readonly bool IsBoss;

        /// <summary>
        /// True when the player stands on this NPC's platform (or either platform is unknown — fail-safe).
        /// Engagement is a local, on-platform act: off-platform NPCs are ineligible entirely.
        /// </summary>
        public readonly bool OnPlayerPlatform;

        public NpcProximitySample(string id, PlanarPoint position, NpcIntent intent, bool consumed,
            bool isBoss = false, bool onPlayerPlatform = true)
        {
            Id = id;
            Position = position;
            Intent = intent;
            Consumed = consumed;
            IsBoss = isBoss;
            OnPlayerPlatform = onPlayerPlatform;
        }
    }

    /// <summary>The frame's proximity decision: which NPC (if any) shows the F prompt, which to aggro, and which boss talks open.</summary>
    public readonly struct ProximityResult
    {
        /// <summary>Id of the single nearest eligible NPC inside the interaction radius (R7), or null.</summary>
        public readonly string NearestPromptNpcId;

        /// <summary>Ids of hostile NPCs the player has crossed into the aggro radius of (R8).</summary>
        public readonly IReadOnlyList<string> AggroNpcIds;

        /// <summary>
        /// Ids of talkable camp bosses whose engagement circle the player has crossed — their dialogue
        /// opens immediately, no F press (a boss's reach is the encounter trigger, not a prompt offer).
        /// </summary>
        public readonly IReadOnlyList<string> AutoTalkNpcIds;

        public ProximityResult(string nearestPromptNpcId, IReadOnlyList<string> aggroNpcIds,
            IReadOnlyList<string> autoTalkNpcIds = null)
        {
            NearestPromptNpcId = nearestPromptNpcId;
            AggroNpcIds = aggroNpcIds ?? System.Array.Empty<string>();
            AutoTalkNpcIds = autoTalkNpcIds ?? System.Array.Empty<string>();
        }
    }

    /// <summary>
    /// Pure XZ geometry for NPC proximity (R5-R8 + camp boss engagement). Hostile NPCs inside the aggro
    /// radius are flagged for auto-battle and never prompt; quest-bearer/plain NPCs inside the interaction
    /// radius compete for the single F prompt, which targets the nearest one. A camp boss uses the larger
    /// engagement radius for both intents and auto-engages on cross: hostile boss → aggro, talkable boss →
    /// auto-talk (never the F-prompt competition). NPCs off the player's platform and consumed NPCs are
    /// ignored. No UnityEngine here.
    /// </summary>
    public sealed class ProximityEvaluator
    {
        public ProximityResult Evaluate(PlanarPoint player, IReadOnlyList<NpcProximitySample> npcs, NpcInteractionSettings settings)
        {
            if (npcs == null || npcs.Count == 0 || settings == null)
            {
                return new ProximityResult(null, System.Array.Empty<string>());
            }

            float interactionSqr = settings.InteractionRadius * settings.InteractionRadius;
            float aggroSqr = settings.AggroRadius * settings.AggroRadius;
            float bossSqr = settings.BossEngagementRadius * settings.BossEngagementRadius;

            string nearestId = null;
            float nearestSqr = float.MaxValue;
            List<string> aggro = null;
            List<string> autoTalk = null;

            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.Consumed || !npc.OnPlayerPlatform)
                {
                    continue;
                }

                float sqr = player.SquaredDistanceTo(npc.Position);

                if (npc.IsBoss)
                {
                    if (sqr <= bossSqr)
                    {
                        if (npc.Intent == NpcIntent.Hostile)
                        {
                            aggro ??= new List<string>();
                            aggro.Add(npc.Id);
                        }
                        else
                        {
                            autoTalk ??= new List<string>();
                            autoTalk.Add(npc.Id);
                        }
                    }

                    continue;
                }

                if (npc.Intent == NpcIntent.Hostile)
                {
                    if (sqr <= aggroSqr)
                    {
                        aggro ??= new List<string>();
                        aggro.Add(npc.Id);
                    }

                    continue;
                }

                if (sqr <= interactionSqr && sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestId = npc.Id;
                }
            }

            return new ProximityResult(
                nearestId,
                (IReadOnlyList<string>)aggro ?? System.Array.Empty<string>(),
                (IReadOnlyList<string>)autoTalk ?? System.Array.Empty<string>());
        }
    }
}
