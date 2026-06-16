using System;
using System.Collections.Generic;
using Core.Logging;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Mutation.Presenter
{
    /// <summary>
    /// Drives the stage-up mutation choice: when digestion reports the character is ready to mutate,
    /// it builds 2-3 options from this stage's dominant archetypes and shows them. The player's pick
    /// swaps a body part on the live character; on success the tally and digestion reset so the next
    /// stage starts from zero. A failed swap (e.g. the rig is not yet assembled) or an empty option
    /// set leaves the stage untouched so the player can keep feeding. Holds no domain state beyond
    /// the currently-offered options. Ability grants from the swapped part are deferred (ROADMAP M1).
    /// </summary>
    public class MutationChoicePresenter : IInitializable, IDisposable
    {
        private readonly IDigestionProgress _digestion;
        private readonly IMutationTally _tally;
        private readonly IMutationOptionBuilder _builder;
        private readonly IMutationOptionCatalog _optionCatalog;
        private readonly IArchetypeCatalog _archetypeCatalog;
        private readonly IMutationCharacter _character;
        private readonly MutationConfig _config;
        private readonly IMutationChoiceView _view;
        private readonly IGameLogger _logger;

        private readonly List<MutationOption> _offered = new List<MutationOption>();
        private bool _isShowing;

        public MutationChoicePresenter(
            IDigestionProgress digestion,
            IMutationTally tally,
            IMutationOptionBuilder builder,
            IMutationOptionCatalog optionCatalog,
            IArchetypeCatalog archetypeCatalog,
            IMutationCharacter character,
            MutationConfig config,
            IMutationChoiceView view,
            IGameLogger logger)
        {
            _digestion = digestion;
            _tally = tally;
            _builder = builder;
            _optionCatalog = optionCatalog;
            _archetypeCatalog = archetypeCatalog;
            _character = character;
            _config = config;
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            _digestion.OnChanged += HandleDigestionChanged;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _digestion.OnChanged -= HandleDigestionChanged;
            _view.OnChoiceSelected -= HandleChoiceSelected;
        }

        private void HandleDigestionChanged()
        {
            // The choice stays up until the player picks; ignore further feeding while it shows.
            if (_isShowing || !_digestion.IsReadyToMutate)
            {
                return;
            }

            var dominant = _tally.Dominant(_config.MaxMutationOptions);
            var equipped = CollectEquippedParts(dominant);
            var options = _builder.Build(dominant, _optionCatalog, equipped, _config.MaxMutationOptions);
            if (options.Count == 0)
            {
                // Ready, but the dominant archetypes offer no (new) parts. Keep feeding; do not reset.
                _logger.Info(
                    "[MutationChoicePresenter] Ready to mutate but no mutation options are available " +
                    "for the dominant archetype(s).");
                return;
            }

            _offered.Clear();
            var viewData = new List<MutationChoiceViewData>(options.Count);
            foreach (var option in options)
            {
                _offered.Add(option);
                viewData.Add(ToViewData(option));
            }

            _view.ShowChoices(viewData);
            _view.SetVisible(true);
            _isShowing = true;
        }

        private void HandleChoiceSelected(int index)
        {
            if (index < 0 || index >= _offered.Count)
            {
                _logger.Warning($"[MutationChoicePresenter] Choice index {index} is out of range.");
                return;
            }

            var option = _offered[index];
            if (!_character.SwapPart(option.SlotId, option.PartId))
            {
                // Keep the choice up so the player can retry once the swap can be applied.
                _logger.Error(
                    $"[MutationChoicePresenter] Failed to swap part '{option.PartId}' into slot " +
                    $"'{option.SlotId}'.");
                return;
            }

            // Hide before resetting: the reset raises digestion OnChanged, and the not-ready guard
            // then keeps us from re-evaluating mid-transition.
            _isShowing = false;
            _offered.Clear();
            _view.SetVisible(false);

            _tally.Reset();
            _digestion.Reset();
        }

        private ISet<string> CollectEquippedParts(IReadOnlyList<string> dominantArchetypeIds)
        {
            var equipped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var archetypeId in dominantArchetypeIds)
            {
                foreach (var option in _optionCatalog.OptionsFor(archetypeId))
                {
                    if (option == null || string.IsNullOrEmpty(option.SlotId))
                    {
                        continue;
                    }

                    if (_character.TryGetEquippedPartId(option.SlotId, out var partId)
                        && !string.IsNullOrEmpty(partId))
                    {
                        equipped.Add(partId);
                    }
                }
            }

            return equipped;
        }

        private MutationChoiceViewData ToViewData(MutationOption option)
        {
            var tint = Color.white;
            if (_archetypeCatalog.TryGet(option.ArchetypeId, out var archetype))
            {
                tint = archetype.Tint;
            }

            _optionCatalog.TryGetIcon(option.PartId, out var icon);
            return new MutationChoiceViewData(option.DisplayName, icon, tint);
        }
    }
}
