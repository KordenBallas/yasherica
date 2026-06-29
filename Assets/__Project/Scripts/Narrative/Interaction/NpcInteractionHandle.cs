using Narrative.Interaction.Core;
using Narrative.Interaction.View;
using Platform;
using UnityEngine;
using CastingModel = global::Narrative.Casting.Core.Casting;

namespace Narrative.Interaction
{
    /// <summary>
    /// The run-scoped link between a placed NPC and the proximity system: where it is (its visual
    /// <see cref="Transform"/>), its placement-time <see cref="NpcIntent"/> and casting,
    /// the platform that owns its encounter, and its overhead view. <see cref="Consumed"/> latches once
    /// the encounter has begun so the NPC neither re-prompts nor re-aggros.
    /// </summary>
    public sealed class NpcInteractionHandle
    {
        public string Id { get; }
        public Transform PositionSource { get; }
        public NpcIntent Intent { get; }
        public CastingModel Casting { get; }
        public IPlatform Platform { get; }
        public NpcContent Npc { get; }
        public INpcOverheadView View { get; }
        public bool Consumed { get; set; }

        public NpcInteractionHandle(string id, Transform positionSource, NpcIntent intent, CastingModel casting,
            IPlatform platform, NpcContent npc, INpcOverheadView view)
        {
            Id = id;
            PositionSource = positionSource;
            Intent = intent;
            Casting = casting;
            Platform = platform;
            Npc = npc;
            View = view;
        }
    }
}
