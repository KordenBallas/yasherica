using System;
using System.Collections.Generic;
using CharacterSystem.Runtime;
using Narrative.Facts.Core;
using Zenject;

namespace CharacterSystem.Integration
{
    /// <summary>
    /// The single writer of the tasted-forms catalog (P4-5 reqs 1–2): whenever the hero's body
    /// changes — initial assembly, part swap, frame change, dormant install — every carried
    /// part id (equipped and dormant) is marked <c>world.&lt;partId&gt;.arena_tasted</c>. The
    /// fact is Meta-horizon, so it persists through the existing meta flush points and survives
    /// death. Subscription discipline mirrors <see cref="World.Races.Integration.RacePassportBinder"/>
    /// (assembly may precede or follow Zenject init); only the hero's bound visual is watched,
    /// preview clones never reach the fact store.
    /// </summary>
    public sealed class TastedFormsRecorder : IInitializable, IDisposable
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IFactStore _facts;

        private IModularCharacter _character;

        public TastedFormsRecorder(ModularCharacterVisual visual, IFactStore facts)
        {
            _visual = visual;
            _facts = facts;
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

        /// <summary>Pure core: marks every carried part id as tasted (idempotent).</summary>
        public static void Record(IEnumerable<string> partIds, IFactStore facts)
        {
            if (partIds == null || facts == null)
            {
                return;
            }

            foreach (var partId in partIds)
            {
                if (!string.IsNullOrEmpty(partId))
                {
                    facts.SetBool(WorldFacts.ArenaTasted, true, partId);
                }
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
            Record(CarriedPartIds(_character), _facts);
        }

        private static IEnumerable<string> CarriedPartIds(IModularCharacter character)
        {
            foreach (var partId in character.EquippedParts.Values)
            {
                yield return partId;
            }

            foreach (var dormant in character.DormantParts)
            {
                if (dormant != null)
                {
                    yield return dormant.Id;
                }
            }
        }
    }
}
