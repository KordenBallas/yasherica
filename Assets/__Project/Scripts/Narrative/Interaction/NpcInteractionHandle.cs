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

        /// <summary>Camp boss: larger engagement radius; crossing it auto-engages (talk or fight).</summary>
        public bool IsBoss { get; }

        /// <summary>
        /// Set when this handle is a spawned enemy body (a lone monster), not a story-cast NPC:
        /// crossing its aggro radius engages the whole platform's fight. Null for NPC handles.
        /// </summary>
        public EnemyContent Enemy { get; }

        public NpcInteractionHandle(string id, Transform positionSource, NpcIntent intent, CastingModel casting,
            IPlatform platform, NpcContent npc, INpcOverheadView view, bool isBoss = false,
            EnemyContent enemy = null)
        {
            Id = id;
            PositionSource = positionSource;
            Intent = intent;
            Casting = casting;
            Platform = platform;
            Npc = npc;
            View = view;
            IsBoss = isBoss;
            Enemy = enemy;
        }
    }
}
