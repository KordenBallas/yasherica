using System;
using System.Collections.Generic;
using Character;
using Core.Events;
using Narrative.Interaction.Core;
using Platform;
using UnityEngine;
using Zenject;

namespace Narrative.Interaction
{
    /// <summary>
    /// Drives NPC proximity each frame (R4-R8 + camp boss engagement): it samples the registered NPCs and
    /// the player position, asks the pure <see cref="ProximityEvaluator"/> for the decision, then shows the
    /// F prompt on the nearest eligible NPC, auto-aggros any hostile in range, auto-opens a crossed boss's
    /// dialogue, and opens a conversation when the player presses interact. Engagement is platform-scoped:
    /// the player's current platform is tracked via <see cref="PlatformEvents"/> and only NPCs on it are
    /// eligible. All geometry lives in the evaluator; this presenter only orchestrates and forwards to
    /// <see cref="NpcEncounterStarter"/>.
    /// </summary>
    public sealed class NpcProximityPresenter : ITickable, IDisposable
    {
        private readonly ICharacterRegistry _characterRegistry;
        private readonly INpcInteractionRegistry _registry;
        private readonly ProximityEvaluator _evaluator;
        private readonly NpcInteractionSettings _settings;
        private readonly IInteractionInput _input;
        private readonly NpcEncounterStarter _starter;

        private readonly List<NpcInteractionHandle> _handleScratch = new List<NpcInteractionHandle>();
        private readonly List<NpcProximitySample> _sampleScratch = new List<NpcProximitySample>();

        private IPlatform _currentPlatform;

        public NpcProximityPresenter(
            ICharacterRegistry characterRegistry,
            INpcInteractionRegistry registry,
            ProximityEvaluator evaluator,
            NpcInteractionSettings settings,
            IInteractionInput input,
            NpcEncounterStarter starter)
        {
            _characterRegistry = characterRegistry;
            _registry = registry;
            _evaluator = evaluator;
            _settings = settings;
            _input = input;
            _starter = starter;

            PlatformEvents.OnPlatformEntered += HandlePlatformEntered;
        }

        public void Dispose()
        {
            PlatformEvents.OnPlatformEntered -= HandlePlatformEntered;
        }

        public void Tick()
        {
            var player = _characterRegistry?.GetPlayerCharacter();
            if (player == null)
            {
                return;
            }

            // Snapshot handles: starting an encounter unregisters NPCs, which would mutate the live list.
            _handleScratch.Clear();
            _handleScratch.AddRange(_registry.Handles);

            _sampleScratch.Clear();
            for (int i = 0; i < _handleScratch.Count; i++)
            {
                var handle = _handleScratch[i];
                if (handle.PositionSource == null)
                {
                    continue;
                }

                _sampleScratch.Add(new NpcProximitySample(handle.Id, ToPlanar(handle.PositionSource.position),
                    handle.Intent, handle.Consumed, handle.IsBoss, IsOnPlayerPlatform(handle)));
            }

            var result = _evaluator.Evaluate(ToPlanar(player.position), _sampleScratch, _settings);

            // Aggro first — a hostile in range pre-empts any prompt this frame.
            var aggroIds = result.AggroNpcIds;
            for (int i = 0; i < aggroIds.Count; i++)
            {
                if (_registry.TryGet(aggroIds[i], out var hostile))
                {
                    _starter.StartAggro(hostile);
                }
            }

            // A crossed boss's talk opens by itself — his circle is the trigger, not a prompt offer.
            var autoTalkIds = result.AutoTalkNpcIds;
            for (int i = 0; i < autoTalkIds.Count; i++)
            {
                if (_registry.TryGet(autoTalkIds[i], out var boss))
                {
                    _starter.StartTalk(boss);
                }
            }

            string nearestId = result.NearestPromptNpcId;
            UpdatePrompts(nearestId);

            if (nearestId != null && _input != null && _input.WasInteractPressedThisFrame()
                && _registry.TryGet(nearestId, out var target))
            {
                _starter.StartTalk(target);
            }
        }

        private void HandlePlatformEntered(IPlatform platform)
        {
            _currentPlatform = platform;
        }

        /// <summary>
        /// Engagement is a local, on-platform act; when either platform is unknown the NPC stays
        /// eligible (fail-safe: never let a tracking gap mute the world).
        /// </summary>
        private bool IsOnPlayerPlatform(NpcInteractionHandle handle)
        {
            return _currentPlatform == null || handle.Platform == null
                || ReferenceEquals(handle.Platform, _currentPlatform);
        }

        private void UpdatePrompts(string nearestId)
        {
            for (int i = 0; i < _handleScratch.Count; i++)
            {
                var handle = _handleScratch[i];
                bool show = !handle.Consumed && handle.Id == nearestId;
                handle.View?.ShowPrompt(show);
            }
        }

        private static PlanarPoint ToPlanar(Vector3 position) => new PlanarPoint(position.x, position.z);
    }
}
