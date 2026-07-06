using System;
using System.Collections.Generic;
using Character;
using Hub.Core;
using Hub.View;
using Narrative.Interaction;
using Zenject;

namespace Hub.Presenter
{
    /// <summary>
    /// Drives the Hub's walk-up interactions each frame (O1 rework): the scene entrypoint
    /// registers the F-spots (the junk-keeper NPC + the biome portals), this presenter samples
    /// the player's position, asks the pure <see cref="HubProximity"/> for the nearest in-range
    /// spot, shows that spot's prompt, and fires the spot's action when interact (F) is pressed.
    /// The NpcProximityPresenter shape, without the Area's registry/aggro/dialogue machinery.
    /// </summary>
    public sealed class HubProximityPresenter : ITickable
    {
        private sealed class Entry
        {
            public HubInteractionSpot Spot;
            public Action OnInteract;
            public IHubPromptView View;
        }

        private readonly ICharacterRegistry _characterRegistry;
        private readonly IInteractionInput _input;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<HubInteractionSpot> _spotScratch = new List<HubInteractionSpot>();

        public HubProximityPresenter(ICharacterRegistry characterRegistry, IInteractionInput input)
        {
            _characterRegistry = characterRegistry;
            _input = input;
        }

        public void Register(HubInteractionSpot spot, Action onInteract, IHubPromptView view)
        {
            if (spot == null)
            {
                return;
            }

            _entries.Add(new Entry { Spot = spot, OnInteract = onInteract, View = view });
        }

        public void Tick()
        {
            var player = _characterRegistry?.GetPlayerCharacter();
            if (player == null || _entries.Count == 0)
            {
                return;
            }

            _spotScratch.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                _spotScratch.Add(_entries[i].Spot);
            }

            var position = player.position;
            int nearest = HubProximity.FindNearest(position.x, position.z, _spotScratch);

            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].View?.ShowPrompt(i == nearest);
            }

            if (nearest >= 0 && _input != null && _input.WasInteractPressedThisFrame())
            {
                _entries[nearest].OnInteract?.Invoke();
            }
        }
    }
}
