using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.View;
using Narrative.Facts.Core;
using UnityEngine;
using Zenject;

namespace Mutation.Presenter
{
    /// <summary>
    /// Drives the unseal variant choice: when the player confirms a complete
    /// medallion (<see cref="IBlankRackView.OnUnsealClicked"/> — the Track F
    /// confirm-before-unseal beat, replacing the old auto-open on the last
    /// socket), it scores the authored parts of the blank's slot against the
    /// socketed reagents and shows the variant cards. The player's pick installs
    /// the part on the live character, consumes the socketed artifacts
    /// (commit-on-unseal), and removes the blank from the rack. A rejected
    /// install (e.g. the rig is not yet assembled) keeps the cards up for a retry.
    ///
    /// Body plans (P2-1): a frame-changing pick can defer behind the shed-confirm
    /// dialog (PendingConfirmation) — the cards stay up behind the modal and the
    /// unseal commits only on a confirmed install; a decline leaves the blank,
    /// sockets, and body untouched. Offers are additionally filtered to parts that
    /// can actually be installed on the current body (CanInstall).
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
        private readonly IBlankRackView _rackView;
        private readonly IGameLogger _logger;
        private readonly Narrative.Barks.Core.ICauldronBarkService _barks;
        private readonly CharacterSystem.Data.IPartCatalog _partDefinitions;
        private readonly Narrative.Facts.Core.IFactStore _facts;

        private readonly List<MutationOption> _offered = new List<MutationOption>();
        private int _shownBlankInstanceId = -1;
        private bool _isShowing;
        private bool _awaitingBodyPlanDecision;
        private MutationOption _pickedOption;

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
            IBlankRackView rackView,
            IGameLogger logger,
            Narrative.Barks.Core.ICauldronBarkService barks = null,
            CharacterSystem.Data.IPartCatalog partDefinitions = null,
            Narrative.Facts.Core.IFactStore facts = null)
        {
            _barks = barks;
            _partDefinitions = partDefinitions;
            _facts = facts;
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
            _rackView = rackView;
            _logger = logger;
        }

        public void Initialize()
        {
            _rackView.OnUnsealClicked += HandleUnsealClicked;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _character.SwapRequestResolved += HandleSwapRequestResolved;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _rackView.OnUnsealClicked -= HandleUnsealClicked;
            _view.OnChoiceSelected -= HandleChoiceSelected;
            _character.SwapRequestResolved -= HandleSwapRequestResolved;
        }

        private void HandleUnsealClicked(int blankInstanceId)
        {
            if (_isShowing)
            {
                return;
            }

            // Confirm-before-unseal: the click is the commit gesture, but only a
            // genuinely complete medallion opens the cards.
            if (!_socketing.IsReady(blankInstanceId))
            {
                _logger.Info(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Unseal of blank {blankInstanceId} rejected " +
                    "(not all sockets are filled).");
                return;
            }

            TryShow(blankInstanceId);
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
                CollectExcludedParts(blank.SlotId),
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
            BarkTemptationIfMonstrous();
            return true;
        }

        /// <summary>
        /// The temptation bark slot (P1-10): the unseal menu surfaced a strong/monstrous option —
        /// any offered variant at/above the authored temptation rarity — so the cauldron purrs.
        /// </summary>
        private void BarkTemptationIfMonstrous()
        {
            if (_barks == null)
            {
                return;
            }

            foreach (var option in _offered)
            {
                if (_partCatalog.TryGetCardData(option.PartId, out var card)
                    && card.RarityTier >= _config.TemptationRarityTier)
                {
                    _barks.Bark(Narrative.Barks.Core.CauldronBarkSlot.Temptation);
                    return;
                }
            }
        }

        private void HandleChoiceSelected(int index)
        {
            if (_awaitingBodyPlanDecision)
            {
                // A body-plan confirm dialog is up; ignore card clicks behind the modal.
                return;
            }

            if (index < 0 || index >= _offered.Count)
            {
                _logger.Warning(LogCategory.Mutation,
                    $"[MutationVariantPresenter] Choice index {index} is out of range.");
                return;
            }

            var option = _offered[index];
            _pickedOption = option;
            switch (_character.RequestSwapPart(option.SlotId, option.PartId))
            {
                case SwapRequestOutcome.Applied:
                    CommitUnseal();
                    break;

                case SwapRequestOutcome.PendingConfirmation:
                    // The shed-confirm dialog is up; the cards stay behind it and the
                    // unseal commits (or not) in HandleSwapRequestResolved.
                    _awaitingBodyPlanDecision = true;
                    break;

                default:
                    // Keep the cards up so the player can retry once the install can be applied.
                    _logger.Error(LogCategory.Mutation,
                        $"[MutationVariantPresenter] Failed to install part '{option.PartId}' into slot " +
                        $"'{option.SlotId}'.");
                    break;
            }
        }

        private void HandleSwapRequestResolved(bool installed)
        {
            if (!_awaitingBodyPlanDecision)
            {
                return;
            }

            _awaitingBodyPlanDecision = false;
            if (installed)
            {
                CommitUnseal();
            }

            // Declined: the body, blank, and sockets are untouched (FR7) - the cards
            // simply stay up so the player can pick a different variant.
        }

        private void CommitUnseal()
        {
            // Commit-on-unseal: the socketed reagents are consumed and the blank is
            // spent; the unchosen variants evaporate with it.
            _socketing.ConsumeSockets(_shownBlankInstanceId);
            _rack.Remove(_shownBlankInstanceId);

            RecordRestraintIfModest(_pickedOption);
            _pickedOption = null;

            _isShowing = false;
            _shownBlankInstanceId = -1;
            _offered.Clear();
            _view.SetVisible(false);
        }

        /// <summary>
        /// The restraint counterpoint (P1-10): installing a marker (race-tagged) or modest
        /// (below-temptation-tier) part leans the run toward friendship — the path_restraint counter
        /// moves and the cauldron sours. The lean is only ever these counters; no new meter.
        /// </summary>
        private void RecordRestraintIfModest(MutationOption option)
        {
            if (option == null)
            {
                return;
            }

            bool isMarker = _partDefinitions != null
                && _partDefinitions.TryGet(option.PartId, out var definition)
                && !string.IsNullOrEmpty(definition.RaceId);
            bool isModest = _partCatalog.TryGetCardData(option.PartId, out var card)
                && card.RarityTier < _config.TemptationRarityTier;
            if (!isMarker && !isModest)
            {
                return;
            }

            _facts?.SetInt(
                Narrative.Facts.Core.WorldFacts.PathRestraint,
                _facts.GetInt(Narrative.Facts.Core.WorldFacts.PathRestraint) + 1);
            _barks?.Bark(Narrative.Barks.Core.CauldronBarkSlot.Restraint);
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

        private IReadOnlyCollection<string> CollectExcludedParts(string slotId)
        {
            var excluded = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(slotId))
            {
                return excluded;
            }

            // The current occupant (active or dormant) is never re-offered.
            if (_character.TryGetEquippedPartId(slotId, out var occupantId)
                && !string.IsNullOrEmpty(occupantId))
            {
                excluded.Add(occupantId);
            }

            // Body plans: parts that cannot be installed on the current body (their bones
            // have no home on the governing frame and they change nothing) are never offered,
            // so every card shown is actually pickable.
            foreach (var candidate in _partCatalog.AllCandidates)
            {
                if (string.Equals(candidate.SlotId, slotId, StringComparison.Ordinal)
                    && !excluded.Contains(candidate.PartId)
                    && !_character.CanInstall(candidate.PartId))
                {
                    excluded.Add(candidate.PartId);
                }
            }

            return excluded;
        }

        private MutationChoiceViewData ToViewData(MutationOption option)
        {
            // The card tint is the blank's species marker (carried on the option).
            var tint = Color.white;
            if (_archetypeCatalog.TryGet(option.ArchetypeId, out var archetype))
            {
                tint = archetype.Tint;
            }

            var front = BuildFace(option.PartId, option.DisplayName, out var rarityTier);

            // The back face is the part this mutation would replace. An empty slot and a
            // not-yet-assembled rig both read as "nothing replaced" (bare-slot back face).
            var hasReplacedPart = false;
            var back = default(MutationCardFaceViewData);
            if (_character.TryGetEquippedPartId(option.SlotId, out var replacedId)
                && !string.IsNullOrEmpty(replacedId)
                && _partCatalog.TryGetCardData(replacedId, out var replacedCard))
            {
                hasReplacedPart = true;
                back = ToFace(replacedCard);
            }

            return new MutationChoiceViewData(
                option.SlotId,
                option.PartId,
                front,
                hasReplacedPart,
                back,
                tint,
                rarityTier);
        }

        private MutationCardFaceViewData BuildFace(
            string partId, string fallbackName, out int rarityTier)
        {
            if (_partCatalog.TryGetCardData(partId, out var card))
            {
                rarityTier = card.RarityTier;
                return ToFace(card);
            }

            // Content gap: the offered part has no catalog card data; degrade to the
            // option's label so the card still renders.
            _logger.Warning(LogCategory.Mutation,
                $"[MutationVariantPresenter] No card data for part '{partId}'; " +
                "showing the option label only.");
            _partCatalog.TryGetIcon(partId, out var icon);
            rarityTier = 0;
            return new MutationCardFaceViewData(
                fallbackName, icon, Array.Empty<MutationAbilityIconViewData>());
        }

        private static MutationCardFaceViewData ToFace(MutationPartCardData card)
        {
            var abilities = new MutationAbilityIconViewData[card.Abilities.Count];
            for (int i = 0; i < card.Abilities.Count; i++)
            {
                var ability = card.Abilities[i];
                abilities[i] = new MutationAbilityIconViewData(
                    ability.Name, ability.Description, ability.Icon, ability.IsPassive,
                    ability.IsLine, ability.LineLength, ability.RingRadius, ability.AnimationTrigger,
                    ability.StatusGlyph);
            }

            return new MutationCardFaceViewData(card.DisplayName, card.Icon, abilities);
        }
    }
}
