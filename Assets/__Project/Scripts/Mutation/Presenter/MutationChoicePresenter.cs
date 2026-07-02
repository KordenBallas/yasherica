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
    /// it scores every candidate part against this stage's feed tally and shows the top options. The
    /// player's pick swaps a body part on the live character; on success the tally and digestion reset
    /// so the next stage starts from zero. A failed swap (e.g. the rig is not yet assembled) or an
    /// empty option set leaves the stage untouched so the player can keep feeding. Holds no domain
    /// state beyond the currently-offered options. The swapped part's abilities are picked up by combat
    /// re-reading the equipped parts at combat start (ability-subsystem.md §2.6).
    /// </summary>
    public class MutationChoicePresenter : IInitializable, IDisposable
    {
        private readonly IDigestionProgress _digestion;
        private readonly IMutationTally _tally;
        private readonly IMutationOptionBuilder _builder;
        private readonly IMutationPartCatalog _partCatalog;
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
            IMutationPartCatalog partCatalog,
            IArchetypeCatalog archetypeCatalog,
            IMutationCharacter character,
            MutationConfig config,
            IMutationChoiceView view,
            IGameLogger logger)
        {
            _digestion = digestion;
            _tally = tally;
            _builder = builder;
            _partCatalog = partCatalog;
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

            var candidates = _partCatalog.AllCandidates;
            var equipped = CollectEquippedParts(candidates);
            var scoring = new MutationScoringParameters(
                _config.RarityWeight, _config.RarityUnlockPointsPerTier);
            var options = _builder.Build(
                _tally.Totals, candidates, equipped, _config.MaxMutationOptions, scoring);
            if (options.Count == 0)
            {
                // Ready, but no unequipped part scores positively against the feed tally. Keep
                // feeding; do not reset.
                _logger.Info(LogCategory.Mutation,
                    "[MutationChoicePresenter] Ready to mutate but no mutation options score against " +
                    "the current feed tally.");
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
                _logger.Warning(LogCategory.Mutation,$"[MutationChoicePresenter] Choice index {index} is out of range.");
                return;
            }

            var option = _offered[index];
            if (!_character.SwapPart(option.SlotId, option.PartId))
            {
                // Keep the choice up so the player can retry once the swap can be applied.
                _logger.Error(LogCategory.Mutation,
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

        private ISet<string> CollectEquippedParts(IReadOnlyList<MutationCandidatePart> candidates)
        {
            // Exclude every part currently worn in a slot a candidate could fill, so a choice never
            // re-offers an equipped part (including the character's starting parts). Slots are visited
            // once even when several candidates share one.
            var equipped = new HashSet<string>(StringComparer.Ordinal);
            var seenSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.SlotId) || !seenSlots.Add(candidate.SlotId))
                {
                    continue;
                }

                if (_character.TryGetEquippedPartId(candidate.SlotId, out var partId)
                    && !string.IsNullOrEmpty(partId))
                {
                    equipped.Add(partId);
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

            _partCatalog.TryGetIcon(option.PartId, out var icon);
            return new MutationChoiceViewData(option.DisplayName, icon, tint);
        }
    }
}
