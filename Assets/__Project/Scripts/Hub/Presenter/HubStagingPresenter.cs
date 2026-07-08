using System;
using System.Collections.Generic;
using Core.Logging;
using Core.Persistence;
using Core.SceneFlow;
using Hub.Core;
using Hub.Data;
using Hub.View;
using LevelGeneration;
using Mutation.Data;
using Mutation.View;
using UnityEngine;
using World.Races.Core;
using Zenject;

namespace Hub.Presenter
{
    /// <summary>
    /// Orchestrates the Hub's staging flow (O1, reworked to the walkable platform): deals the
    /// starting-part offer (a deterministic draw over the tasted pool, salted by the upcoming
    /// run's index) and holds it until the player TALKS to the junk-keeper (<see cref="ShowOffer"/>
    /// re-opens the shared card panel — the pick can be revised until launch); derives the
    /// enterable homelands for the portals; and commits the launch when a portal is used
    /// (<see cref="LaunchInto"/>) — write the one-shot run setup, consume any abandoned run save
    /// (the Journey delete moved HERE from the main menu), and load the Area. Walking into a
    /// portal bare is always allowed. The wide constructor is this class's single responsibility:
    /// it is the one place that knows what "staging a run" is.
    /// </summary>
    public sealed class HubStagingPresenter : IInitializable, IDisposable
    {
        private const string BareLaunchLabel = "—";

        private readonly HubStagingModel _model;
        private readonly IHubStagingView _view;
        private readonly IStartingPartPoolSource _poolSource;
        private readonly StartingPartSelector _selector;
        private readonly IMutationPartCatalog _cardCatalog;
        private readonly IRaceTintCatalog _tints;
        private readonly HubMetaReader _meta;
        private readonly IRaceRoster _races;
        private readonly IRunSetupStore _setupStore;
        private readonly IRunSaveStore _runSaveStore;
        private readonly ISceneLoader _sceneLoader;
        private readonly IGameLogger _logger;
        private readonly IMutationChoiceView _choiceView;
        private readonly MetaProgression.Core.MetaProgressionSettings _settings;
        private readonly MetaProgression.Core.DirectionProfile _direction;
        private readonly HubPanelArbiter _panelArbiter;
        private readonly HubHeatModel _heatModel;
        private readonly Heat.Core.HeatSettings _heatSettings;

        private readonly List<HubHomeland> _homelands = new List<HubHomeland>();
        private readonly List<MutationChoiceViewData> _cards = new List<MutationChoiceViewData>();

        private bool _launched;

        public HubStagingPresenter(
            HubStagingModel model,
            IHubStagingView view,
            IStartingPartPoolSource poolSource,
            StartingPartSelector selector,
            IMutationPartCatalog cardCatalog,
            IRaceTintCatalog tints,
            HubMetaReader meta,
            IRaceRoster races,
            IRunSetupStore setupStore,
            IRunSaveStore runSaveStore,
            ISceneLoader sceneLoader,
            IGameLogger logger,
            IMutationChoiceView choiceView = null,
            MetaProgression.Core.MetaProgressionSettings settings = null,
            MetaProgression.Core.DirectionProfile direction = null,
            HubPanelArbiter panelArbiter = null,
            HubHeatModel heatModel = null,
            Heat.Core.HeatSettings heatSettings = null)
        {
            _model = model;
            _view = view;
            _poolSource = poolSource;
            _selector = selector;
            _cardCatalog = cardCatalog;
            _tints = tints;
            _meta = meta;
            _races = races;
            _setupStore = setupStore;
            _runSaveStore = runSaveStore;
            _sceneLoader = sceneLoader;
            _logger = logger;
            _choiceView = choiceView;
            _settings = settings ?? MetaProgression.Core.MetaProgressionSettings.Defaults;
            _direction = direction ?? MetaProgression.Core.DirectionProfile.Neutral;
            _panelArbiter = panelArbiter;
            _heatModel = heatModel;
            _heatSettings = heatSettings;
        }

        /// <summary>The enterable homelands, portal-per-entry (built at initialize).</summary>
        public IReadOnlyList<HubHomeland> Homelands => _homelands;

        public void Initialize()
        {
            BuildHomelands();
            BuildOffer();

            if (_choiceView != null)
            {
                _choiceView.OnChoiceSelected += HandlePartPicked;
                _choiceView.SetVisible(false);
            }

            if (_heatModel != null)
            {
                _heatModel.PactChanged += HandlePactChanged;
            }

            _view.SetChosenPartLabel(BareLaunchLabel);
        }

        public void Dispose()
        {
            if (_choiceView != null)
            {
                _choiceView.OnChoiceSelected -= HandlePartPicked;
            }

            if (_heatModel != null)
            {
                _heatModel.PactChanged -= HandlePactChanged;
            }
        }

        /// <summary>
        /// The junk-keeper interaction: (re-)opens the card panel over the dealt offer. A pick can
        /// be revised by talking again until launch; an empty offer or a missing panel is a no-op
        /// (the cauldron's empty-offer line already covered the cold start).
        /// </summary>
        public void ShowOffer()
        {
            if (_launched || _choiceView == null || _cards.Count == 0)
            {
                return;
            }

            _panelArbiter?.Claim(this);
            _choiceView.ShowChoices(_cards);
            _choiceView.SetVisible(true);
        }

        /// <summary>
        /// A portal interaction: the launch commit point. Saves the one-shot setup (the chosen
        /// part — empty = bare — and the portal's homeland), consumes any abandoned run save
        /// (revised P2-2 PO decision — not at the menu click), and loads the Area.
        /// </summary>
        public void LaunchInto(LevelTheme homeland)
        {
            if (_launched)
            {
                return;
            }

            _launched = true;
            _model.ChooseBiome(homeland);
            var pact = _heatModel?.BuildPact() ?? Heat.Core.HeatPact.None;
            _setupStore.Save(new RunSetupSnapshot
            {
                StartingPartId = _model.ChosenPartId,
                StartingBiome = homeland.ToString(),
                Heat = Heat.Integration.HeatPactDtoMapper.ToDtos(pact)
            });
            _runSaveStore.Delete();
            _logger.Info(LogCategory.Core,
                $"[HubStagingPresenter] Launching: part='{_model.ChosenPartId}' " +
                $"biome={homeland} heat={pact.TotalHeat} " +
                $"(bare={string.IsNullOrEmpty(_model.ChosenPartId)}).");
            _model.NotifyLaunching();
            _sceneLoader.Load(SceneNames.Area);
        }

        private void BuildHomelands()
        {
            _homelands.Clear();
            var seenThemes = new HashSet<LevelTheme>();
            foreach (var race in _races.All)
            {
                if (!seenThemes.Add(race.HomeBiome))
                {
                    continue;
                }

                _homelands.Add(new HubHomeland(
                    race.HomeBiome,
                    $"{race.HomeBiome} — {race.DisplayName} homeland",
                    race.HomeBiome.ToString()));
            }

            if (_homelands.Count == 0)
            {
                // A raceless world (empty roster) still needs an entry portal per starting biome
                // so direct editor play keeps working.
                _logger.Warning(LogCategory.Core,
                    "[HubStagingPresenter] Empty race roster; raising the three starting-biome portals plain.");
                foreach (var theme in new[] { LevelTheme.Forest, LevelTheme.Desert, LevelTheme.Mountain })
                {
                    _homelands.Add(new HubHomeland(theme, theme.ToString(), theme.ToString()));
                }
            }
        }

        private void BuildOffer()
        {
            int upcomingRunIndex = _meta.ReadRunCount() + 1;
            // Heat lifts the dig dials (bias strength / reserve slot) but never the ceiling — the
            // settings constructor re-clamps, so the never-guarantee holds at any heat (FR6).
            var effective = Heat.Core.HeatDialAdjuster.Apply(
                _settings, _heatSettings, _heatModel?.TotalHeat ?? 0);
            var offer = _selector.Draw(
                _poolSource.BuildPool(), effective.DigOfferSize, upcomingRunIndex,
                _direction, effective);
            _model.SetOffer(offer);

            _cards.Clear();
            foreach (var candidate in offer)
            {
                _cards.Add(ToCard(candidate));
            }
        }

        /// <summary>
        /// A pact change re-deals the dig (heat-ascension FR6): the heat-aware vocabulary re-answers
        /// min-Heat gates and relieved floors in the pool, and the lifted dials re-bias the draw —
        /// deterministically per (pact, seed), same seed as the original deal. The current pick is
        /// dropped (its card may no longer be offered); the visible re-deal is the feature — danger
        /// buys becoming, immediately legible at the cauldron.
        /// </summary>
        private void HandlePactChanged()
        {
            if (_launched)
            {
                return;
            }

            _model.ClearChosenPart();
            _view.SetChosenPartLabel(BareLaunchLabel);
            BuildOffer();
        }

        private MutationChoiceViewData ToCard(StartingPartCandidate candidate)
        {
            MutationCardFaceViewData front;
            int rarityTier = 0;
            if (_cardCatalog.TryGetCardData(candidate.PartId, out var card))
            {
                rarityTier = card.RarityTier;
                var abilities = new MutationAbilityIconViewData[card.Abilities.Count];
                for (int i = 0; i < card.Abilities.Count; i++)
                {
                    var ability = card.Abilities[i];
                    abilities[i] = new MutationAbilityIconViewData(
                        ability.Name, ability.Description, ability.Icon, ability.IsPassive,
                        ability.IsLine, ability.LineLength, ability.RingRadius,
                        ability.AnimationTrigger);
                }

                front = new MutationCardFaceViewData(card.DisplayName, card.Icon, abilities);
            }
            else
            {
                _logger.Warning(LogCategory.Core,
                    $"[HubStagingPresenter] No card data for starting part '{candidate.PartId}'; " +
                    "showing the id only.");
                front = new MutationCardFaceViewData(candidate.PartId, null,
                    Array.Empty<MutationAbilityIconViewData>());
            }

            // A launch installs into an empty (freshly reformed) slot — no back face to flip to.
            return new MutationChoiceViewData(
                candidate.SlotId, candidate.PartId, front,
                hasReplacedPart: false, back: default,
                tint: _tints.TintFor(candidate.RaceId),
                rarityTier: rarityTier);
        }

        private void HandlePartPicked(int index)
        {
            // The panel is shared with the Heat pact — a selection while the pact owns it is theirs.
            if (_panelArbiter != null && !_panelArbiter.IsOwner(this))
            {
                return;
            }

            if (!_model.ChoosePart(index))
            {
                _logger.Warning(LogCategory.Core,
                    $"[HubStagingPresenter] Ignored an out-of-range part pick ({index}).");
                return;
            }

            var chosen = _model.ChosenPart;
            string label = _cardCatalog.TryGetCardData(chosen.PartId, out var card)
                ? card.DisplayName
                : chosen.PartId;
            _view.SetChosenPartLabel(label);
            _choiceView?.SetVisible(false);
        }
    }
}
