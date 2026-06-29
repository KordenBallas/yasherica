using Narrative.Interaction.View;
using Platform;
using UnityEngine;

namespace Narrative.Interaction
{
    /// <summary>
    /// Binds a placed <see cref="NpcContent"/> into the proximity system: spawns its overhead view above
    /// the model, seeds the name + intent marker, and registers a <see cref="NpcInteractionHandle"/>.
    /// Keeps <see cref="NpcContent"/> a thin adapter — it only calls <see cref="Bind"/>/<see cref="Unbind"/>.
    /// This is the infrastructure seam that touches GameObjects; the presenter stays logic-only.
    /// </summary>
    public sealed class NpcInteractionService : INpcInteractionService
    {
        private const float OverheadLocalHeight = 2.2f;

        private readonly INpcInteractionRegistry _registry;

        public NpcInteractionService(INpcInteractionRegistry registry)
        {
            _registry = registry;
        }

        public void Bind(NpcContent npc)
        {
            if (npc?.Actor == null || npc.NpcVisual == null)
            {
                return;
            }

            INpcOverheadView view = CreateView(npc.NpcVisual.transform);
            view?.SetName(npc.Actor.ChosenDisplayName);
            view?.SetIntentMarker(npc.Intent);
            view?.ShowPrompt(false);

            var handle = new NpcInteractionHandle(npc.Actor.InstanceId, npc.NpcVisual.transform, npc.Intent,
                npc.Casting, npc.OwningPlatform, npc, view);
            _registry.Register(handle);
        }

        public void Unbind(NpcContent npc)
        {
            if (npc?.Actor == null)
            {
                return;
            }

            if (_registry.TryGet(npc.Actor.InstanceId, out var handle))
            {
                handle.View?.DestroyView();
            }

            _registry.Unregister(npc.Actor.InstanceId);
        }

        private INpcOverheadView CreateView(Transform anchor)
        {
            var go = new GameObject("NpcOverhead");
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = Vector3.up * OverheadLocalHeight;
            return go.AddComponent<NpcOverheadView>();
        }
    }
}
