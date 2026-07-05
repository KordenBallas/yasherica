using System;
using CharacterSystem.Runtime;
using Zenject;

namespace World.Races.Integration
{
    /// <summary>
    /// Wires the hero's body to the passport: on assembly and after every part swap it hands the
    /// live equipped-part snapshot to <see cref="RacePassportProjector"/>. The rig assembles in the
    /// visual's <c>Start</c>, which may run before or after Zenject's <c>Initialize</c>, so both
    /// orders are covered: subscribe to <see cref="ModularCharacterVisual.CharacterAssembled"/> and
    /// also handle an already-assembled character. Only the hero's bound visual is watched — the
    /// mutation preview clones never reach the fact store.
    /// </summary>
    public sealed class RacePassportBinder : IInitializable, IDisposable
    {
        private readonly ModularCharacterVisual _visual;
        private readonly RacePassportProjector _projector;

        private IModularCharacter _character;

        public RacePassportBinder(ModularCharacterVisual visual, RacePassportProjector projector)
        {
            _visual = visual;
            _projector = projector;
        }

        public void Initialize()
        {
            if (_visual == null)
            {
                return;
            }

            _visual.CharacterAssembled += HandleAssembled;
            if (_visual.Character != null)
            {
                HandleAssembled(_visual.Character);
            }
        }

        public void Dispose()
        {
            if (_visual != null)
            {
                _visual.CharacterAssembled -= HandleAssembled;
            }

            if (_character != null)
            {
                _character.PartsChanged -= HandlePartsChanged;
                _character = null;
            }
        }

        private void HandleAssembled(IModularCharacter character)
        {
            if (character == null || ReferenceEquals(character, _character))
            {
                return;
            }

            if (_character != null)
            {
                _character.PartsChanged -= HandlePartsChanged;
            }

            _character = character;
            _character.PartsChanged += HandlePartsChanged;
            HandlePartsChanged();
        }

        private void HandlePartsChanged()
        {
            _projector.Recompute(_character.EquippedParts.Values);
        }
    }
}
