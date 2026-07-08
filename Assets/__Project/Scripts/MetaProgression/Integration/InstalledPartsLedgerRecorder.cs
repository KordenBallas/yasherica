using System;
using System.Collections.Generic;
using CharacterSystem.Runtime;
using MetaProgression.Core;
using Narrative.Facts.Core;
using Zenject;

namespace MetaProgression.Integration
{
    /// <summary>
    /// The part half of the cross-run direction ledger (meta-progression FR8): whenever the hero's
    /// body changes, every currently EQUIPPED part id (installed — dormant stash excluded, unlike
    /// the tasted catalog) is recorded under the current run index. Subscription discipline mirrors
    /// <see cref="CharacterSystem.Integration.TastedFormsRecorder"/> (assembly may precede or
    /// follow Zenject init); the run index is read per event so the recorder needs no ordering
    /// against <c>RunCounterService</c>.
    /// </summary>
    public sealed class InstalledPartsLedgerRecorder : IInitializable, IDisposable
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IFactStore _facts;
        private readonly RunLedger _ledger;

        private IModularCharacter _character;

        public InstalledPartsLedgerRecorder(ModularCharacterVisual visual, IFactStore facts, RunLedger ledger)
        {
            _visual = visual;
            _facts = facts;
            _ledger = ledger;
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

        /// <summary>Pure core: records every installed part id under the run index (idempotent).</summary>
        public static void Record(IEnumerable<string> partIds, int runIndex, RunLedger ledger)
        {
            if (partIds == null || ledger == null)
            {
                return;
            }

            foreach (var partId in partIds)
            {
                ledger.RecordInstalledPart(runIndex, partId);
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
            Record(_character.EquippedParts.Values, (int)_facts.GetInt(WorldFacts.RunCount), _ledger);
        }
    }
}
