using System;
using System.Collections.Generic;
using Core.Logging;
using Heat.Core;
using Hub.Core;
using Hub.Data;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Hub.Presenter
{
    /// <summary>
    /// The cauldron's dare (heat-ascension FR1–FR3): drives the Heat pact over the Hub's SHARED
    /// mutation card panel with zero view changes — one card per modifier (rank = the rarity glow,
    /// pips + Heat in the name, the rank's rule text as a passive-ability hover) plus a closing
    /// "Seal the pact" card. Confirming a modifier card cycles its rank and re-deals the panel in
    /// place; confirming the seal card closes it and seals the pact. Panel ownership is arbitrated
    /// with the part-offer presenter (<see cref="HubPanelArbiter"/>) — the two share one selection
    /// event. Rank-cycling costs the panel's two-step confirm per step: accepted MVP clunk.
    /// </summary>
    public sealed class HeatPactPresenter : IInitializable, IDisposable
    {
        private const string RankPipTaken = "●";
        private const string RankPipFree = "○";

        private readonly HubHeatModel _model;
        private readonly HubPanelArbiter _arbiter;
        private readonly IGameLogger _logger;
        private readonly IMutationChoiceView _choiceView;
        private readonly HeatPactCardStyle _style;

        private readonly List<MutationChoiceViewData> _cards = new List<MutationChoiceViewData>();

        public HeatPactPresenter(
            HubHeatModel model,
            HubPanelArbiter arbiter,
            IGameLogger logger,
            IMutationChoiceView choiceView = null,
            HeatPactCardStyle style = null)
        {
            _model = model;
            _arbiter = arbiter;
            _logger = logger;
            _choiceView = choiceView;
            _style = style ?? new HeatPactCardStyle(Color.white);
        }

        /// <summary>True when a pact can be offered at all (an authored menu + the shared panel).</summary>
        public bool HasMenu => _choiceView != null && _model.Menu.Count > 0;

        public void Initialize()
        {
            if (_choiceView != null)
            {
                _choiceView.OnChoiceSelected += HandleSelected;
            }
        }

        public void Dispose()
        {
            if (_choiceView != null)
            {
                _choiceView.OnChoiceSelected -= HandleSelected;
            }
        }

        /// <summary>The cauldron interaction: (re-)opens the pact panel. Revisable until launch.</summary>
        public void ShowPact()
        {
            if (!HasMenu)
            {
                return;
            }

            _arbiter.Claim(this);
            Deal();
            _model.NotifyOpened();
        }

        private void HandleSelected(int index)
        {
            if (!_arbiter.IsOwner(this))
            {
                return;
            }

            if (index >= 0 && index < _model.Menu.Count)
            {
                _model.CycleRank(_model.Menu[index].Id);
                Deal();
                return;
            }

            if (index == _model.Menu.Count)
            {
                _choiceView.SetVisible(false);
                _model.Seal();
                return;
            }

            _logger.Warning(LogCategory.Core,
                $"[HeatPactPresenter] Ignored an out-of-range pact selection ({index}).");
        }

        private void Deal()
        {
            _cards.Clear();
            foreach (var modifier in _model.Menu)
            {
                _cards.Add(ToCard(modifier));
            }

            _cards.Add(SealCard());
            _choiceView.ShowChoices(_cards);
            _choiceView.SetVisible(true);
        }

        private MutationChoiceViewData ToCard(HeatModifier modifier)
        {
            int rank = _model.RankOf(modifier.Id);
            // The hover text reads the CURRENT rank's rule (or previews the first step when untaken).
            var shownRank = modifier.Ranks[rank > 0 ? rank - 1 : 0];
            string abilityName = rank > 0
                ? $"Rank {rank}/{modifier.MaxRank} — Heat {modifier.HeatAtRank(rank)}"
                : $"Untaken — first step +{shownRank.HeatValue} Heat";
            string description = shownRank.Description;
            // A cap refusal must never be silent (the 2026-07-08 playtest bug: the capped step
            // simply did nothing) — the card says WHY the next step won't take.
            if (_model.NextStepIsCapped(modifier.Id))
            {
                abilityName += " · next step over the cap";
                description += $"\nThe next step would pass the pact's cap ({_model.SoftCap}) — lower another pain first.";
            }

            var abilities = new[]
            {
                new MutationAbilityIconViewData(abilityName, description, null, isPassive: true)
            };
            var front = new MutationCardFaceViewData(
                $"{modifier.DisplayName}  {Pips(rank, modifier.MaxRank)}", null, abilities);

            // Rank drives the rarity glow — the pact card heats up as ranks are taken.
            return new MutationChoiceViewData(
                modifier.Id, modifier.Id, front,
                hasReplacedPart: false, back: default,
                tint: _style.PactCardTint,
                rarityTier: rank);
        }

        private MutationChoiceViewData SealCard()
        {
            string title = _model.SoftCap > 0
                ? $"Seal the pact — Heat {_model.TotalHeat} / cap {_model.SoftCap}"
                : $"Seal the pact — Heat {_model.TotalHeat}";
            var front = new MutationCardFaceViewData(
                title, null,
                Array.Empty<MutationAbilityIconViewData>());
            return new MutationChoiceViewData(
                "heat-seal", "heat-seal", front,
                hasReplacedPart: false, back: default,
                tint: _style.PactCardTint,
                rarityTier: 0);
        }

        private static string Pips(int rank, int maxRank)
        {
            var pips = new System.Text.StringBuilder(maxRank);
            for (int i = 0; i < maxRank; i++)
            {
                pips.Append(i < rank ? RankPipTaken : RankPipFree);
            }

            return pips.ToString();
        }
    }
}
