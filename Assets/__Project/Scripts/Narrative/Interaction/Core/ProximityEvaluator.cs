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

        public NpcProximitySample(string id, PlanarPoint position, NpcIntent intent, bool consumed)
        {
            Id = id;
            Position = position;
            Intent = intent;
            Consumed = consumed;
        }
    }

    /// <summary>The frame's proximity decision: which NPC (if any) shows the F prompt, and which to aggro.</summary>
    public readonly struct ProximityResult
    {
        /// <summary>Id of the single nearest eligible NPC inside the interaction radius (R7), or null.</summary>
        public readonly string NearestPromptNpcId;

        /// <summary>Ids of hostile NPCs the player has crossed into the aggro radius of (R8).</summary>
        public readonly IReadOnlyList<string> AggroNpcIds;

        public ProximityResult(string nearestPromptNpcId, IReadOnlyList<string> aggroNpcIds)
        {
            NearestPromptNpcId = nearestPromptNpcId;
            AggroNpcIds = aggroNpcIds ?? System.Array.Empty<string>();
        }
    }

    /// <summary>
    /// Pure XZ geometry for NPC proximity (R5-R8). Hostile NPCs inside the aggro radius are flagged for
    /// auto-battle and never prompt; quest-bearer/plain NPCs inside the interaction radius compete for the
    /// single F prompt, which targets the nearest one. Consumed NPCs are ignored. No UnityEngine here.
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

            string nearestId = null;
            float nearestSqr = float.MaxValue;
            List<string> aggro = null;

            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.Consumed)
                {
                    continue;
                }

                float sqr = player.SquaredDistanceTo(npc.Position);

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

            return new ProximityResult(nearestId, (IReadOnlyList<string>)aggro ?? System.Array.Empty<string>());
        }
    }
}
