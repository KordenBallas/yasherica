using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Mutation.Presenter
{
    /// <summary>
    /// Drives the unseal variant choice: when a blank's last socket is filled
    /// (<see cref="ISocketingModel.OnBlankReady"/>), it scores the authored parts of
    /// the blank's slot against the socketed reagents and shows the variant cards.
    /// The player's pick swaps the part on the live character, consumes the
    /// socketed artifacts (commit-on-unseal), and removes the blank from the rack.
    /// A failed swap (e.g. the rig is not yet assembled) keeps the cards up for a
    /// retry. Blanks that ripen while a menu is showing queue and open next.
    /// </summary>
    public class MutationVariantPresenter : IInitializable, IDisposable
    {
        private readonly ISocketingModel _socketing;
        private readonly IBlankRack _rack;
        private readonly IPartBlankDataSource _blankData;
        private readonly IArtifactTraitSource _traitSource;
        private readonly IBlankVariantBuilder _builder;
        private readonly IMutationPartCatalog _partCatalog;
        private readonly IArchetypeCatalog _archetypeCatalog;
        private readonly IMutationCharacter _character;
        private readonly MutationConfig _config;
        private readonly IMutationChoiceView _view;
        private readonly IGameLogger _logger;

        private readonly List<MutationOption> _offered = new List<MutationOption>();
        private readonly Queue<int> _readyBlanks = new Queue<int>();
        private int _shownBlankInstanceId = -1;
        private bool _isShowing;

        public MutationVariantPresenter(
            ISocketingModel socketing,
            IBlankRack rack,
            IPartBlankDataSource blankData,
            IArtifactTraitSource traitSource,
            IBlankVariantBuilder builder,
            IMutationPartCatalog partCatalog,
            IArchetypeCatalog archetypeCatalog,
            IMutationCharacter character,
            MutationConfig config,
            IMutationChoiceView view,
            IGameLogger logger)
        {
            _socketing = socketing;
            _rack = rack;
            _blankData = blankData;
            _traitSource = traitSource;
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
            _socketing.OnBlankReady += HandleBlankReady;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _socketing.OnBlankReady -= HandleBlankReady;
            _view.OnChoiceSelected -= HandleChoiceSelected;
        }

        private void HandleBlankReady(int blankInstanceId)
        {
            _readyBlanks.Enqueue(blankInstanceId);
            TryShowNext();
        }

        private void TryShowNext()
        {
            while (!_isShowing && _readyBlanks.Count > 0)
            {
                int blankInstanceId = _readyBlanks.Dequeue();
                if (TryShow(blankInstanceId))
                {
                    return;
                }
            }
        }

        private bool TryShow(int blankInstanceId)
        {
            if (!_rack.TryGet(blankInstanceId, out var instance)
                || !_blankData.TryGet(instance.DefinitionId, out var blank))
            {
                _logger.Warning(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Ready blank {blankInstanceId} is unknown to the rack " +
                    "or has no definition.");
                return false;
            }

            var options = _builder.Build(
                blank,
                CollectSocketedProfiles(blankInstanceId),
                _partCatalog.AllCandidates,
                CollectEquippedPart(blank.SlotId),
                _config.MaxVariantOptions,
                new VariantScoringParameters(_config.RarityWeight, _config.TierUnlockPerRarityTier));
            if (options.Count == 0)
            {
                // Nothing authored (or everything equipped) for this slot; the blank
                // stays committed - a content gap the validator warns about.
                _logger.Warning(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Blank '{blank.DefinitionId}' unsealed with no variant " +
                    $"options for slot '{blank.SlotId}'.");
                return false;
            }

            _offered.Clear();
            var viewData = new List<MutationChoiceViewData>(options.Count);
            foreach (var option in options)
            {
                _offered.Add(option);
                viewData.Add(ToViewData(option));
            }

            _shownBlankInstanceId = blankInstanceId;
            _view.ShowChoices(viewData);
            _view.SetVisible(true);
            _isShowing = true;
            return true;
        }

        private void HandleChoiceSelected(int index)
        {
            if (index < 0 || index >= _offered.Count)
            {
                _logger.Warning(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Choice index {index} is out of range.");
                return;
            }

            var option = _offered[index];
            if (!_character.SwapPart(option.SlotId, option.PartId))
            {
                // Keep the cards up so the player can retry once the swap can be applied.
                _logger.Error(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Failed to swap part '{option.PartId}' into slot " +
                    $"'{option.SlotId}'.");
                return;
            }

            // Commit-on-unseal: the socketed reagents are consumed and the blank is
            // spent; the unchosen variants evaporate with it.
            _socketing.ConsumeSockets(_shownBlankInstanceId);
            _rack.Remove(_shownBlankInstanceId);

            _isShowing = false;
            _shownBlankInstanceId = -1;
            _offered.Clear();
            _view.SetVisible(false);

            TryShowNext();
        }

        private IReadOnlyList<ArtifactTraitProfile> CollectSocketedProfiles(int blankInstanceId)
        {
            var socketed = _socketing.SocketedArtifacts(blankInstanceId);
            var profiles = new List<ArtifactTraitProfile>(socketed.Count);
            foreach (var artifact in socketed)
            {
                if (_traitSource.TryGetProfile(artifact.DefinitionId, out var profile))
                {
                    profiles.Add(profile);
                }
            }

            return profiles;
        }

        private IReadOnlyCollection<string> CollectEquippedPart(string slotId)
        {
            if (!string.IsNullOrEmpty(slotId)
                && _character.TryGetEquippedPartId(slotId, out var partId)
                && !string.IsNullOrEmpty(partId))
            {
                return new[] { partId };
            }

            return Array.Empty<string>();
        }

        private MutationChoiceViewData ToViewData(MutationOption option)
        {
            // The card tint is the blank's species marker (carried on the option).
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
