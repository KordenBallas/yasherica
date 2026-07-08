using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Core.Logging;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using GameInput.Core;
using Hub.Core;
using Hub.Presenter;
using UnityEngine;
using Zenject;

namespace Hub.View
{
    /// <summary>
    /// Assembles the Hub's world (O1 rework): builds the one junkyard platform, plants the
    /// junk-keeper NPC slightly LEFT of the platform centre (talking to him deals the
    /// starting-part cards), and raises one labelled portal per enterable homeland on the far
    /// arc (walking up + F launches the run into that biome). Everything is registered as an
    /// F-spot with the proximity presenter; the hero itself is the scene's prefab instance and
    /// simply walks the platform.
    /// </summary>
    public sealed class HubSceneEntrypoint : IInitializable, IDisposable
    {
        private const string NpcAssemblyResourcePath = "CharacterSystem/Assemblies/PlaceholderAssembly_A";
        private const string TalkPromptVerb = "Talk";
        private const string CauldronDisplayName = "The Cauldron";
        private const string DarePromptVerb = "Dare";

        // The world camera (Cinemachine isometric vcam, euler 30/45/0) maps the screen axes onto
        // the world diagonals: screen-left = (−X, +Z), screen-up/far = (+X, +Z). All placement is
        // expressed in this basis so "left of centre" is left AS SEEN, not a guessed world axis.
        private static readonly Vector3 ScreenLeft = new Vector3(-0.70710678f, 0f, 0.70710678f);

        /// <summary>The keeper stands just left of centre on screen.</summary>
        private const float KeeperScreenLeftDistance = 1.8f;

        /// <summary>The cauldron mirrors the keeper on screen-right — the dare faces the deal.</summary>
        private const float CauldronScreenRightDistance = 1.8f;

        private static readonly Vector3 CauldronScale = new Vector3(1.1f, 0.45f, 1.1f);

        private const float OverheadLabelHeight = 2.2f;
        /// <summary>Portals straddle the far (screen-up) arc: in the (sin a, 0, cos a) ring
        /// parametrization screen-far is 45°, so the three sit symmetric around it (−5°/45°/95°).
        /// A wide step + a generous radius keep the intended cells apart; the claimed-cell dedup
        /// (see <see cref="SnapToFreeCell"/>) then GUARANTEES no two spots share a cell.</summary>
        private const float PortalArcStartDegrees = -5f;
        private const float PortalArcStepDegrees = 50f;
        private const float PortalRingFraction = 0.72f;
        private const float MinPortalRingRadius = 3.5f;

        private static readonly Vector3 PortalDiscScale = new Vector3(1.5f, 0.05f, 1.5f);

        /// <summary>Cells already occupied by a placed spot (keeper + portals), so the next spot
        /// never snaps onto an occupied cell — the fix for portals collapsing into one.</summary>
        private readonly HashSet<HexCoordinates> _claimedCells = new HashSet<HexCoordinates>();

        private readonly HubPlatformAssembler _platformAssembler;
        private readonly Data.HubSceneConfig _config;
        private readonly IModularCharacterFactory _characterFactory;
        private readonly HubStagingPresenter _staging;
        private readonly HubProximityPresenter _proximity;
        private readonly IPromptCueProvider _cues;
        private readonly IGameLogger _logger;
        private readonly HeatPactPresenter _heatPact;
        private readonly Data.HeatPactCardStyle _heatStyle;

        /// <summary>Every overhead label with a device-dependent prompt and its verb ("Talk", portal
        /// name), so a device switch re-renders them all at once.</summary>
        private readonly List<(HubOverheadLabelView View, string Verb)> _promptedLabels =
            new List<(HubOverheadLabelView, string)>();

        public HubSceneEntrypoint(
            HubPlatformAssembler platformAssembler,
            Data.HubSceneConfig config,
            IModularCharacterFactory characterFactory,
            HubStagingPresenter staging,
            HubProximityPresenter proximity,
            IPromptCueProvider cues,
            IGameLogger logger,
            HeatPactPresenter heatPact = null,
            Data.HeatPactCardStyle heatStyle = null)
        {
            _platformAssembler = platformAssembler;
            _config = config;
            _characterFactory = characterFactory;
            _staging = staging;
            _proximity = proximity;
            _cues = cues;
            _logger = logger;
            _heatPact = heatPact;
            _heatStyle = heatStyle;
        }

        public void Initialize()
        {
            var platform = _platformAssembler.Build();
            var center = global::Platform.PlatformAnchor.CenterCellWorld(
                platform.Surface, platform.GameObject.transform.position);
            float extent = PlanarExtent(platform, center);

            PlaceKeeper(platform, center);
            PlaceCauldron(platform, center);
            PlacePortals(platform, center, extent);

            _cues.CuesChanged += RefreshPrompts;
        }

        public void Dispose()
        {
            _cues.CuesChanged -= RefreshPrompts;
        }

        private void PlaceKeeper(HubPlatform platform, Vector3 center)
        {
            var root = new GameObject("HubKeeper");
            root.transform.SetParent(platform.GameObject.transform);
            // Snapped to the nearest unblocked, unclaimed cell (also reserves it so no portal lands
            // on the keeper).
            root.transform.position = SnapToFreeCell(platform, center + ScreenLeft * KeeperScreenLeftDistance);

            var assembly = Resources.Load<CharacterAssemblyDefinition>(NpcAssemblyResourcePath);
            if (assembly != null)
            {
                if (_characterFactory.Create(assembly, root.transform) == null)
                {
                    _logger.Warning(LogCategory.Core,
                        "[HubSceneEntrypoint] Keeper body assembly failed; the spot works label-only.");
                }
            }
            else
            {
                _logger.Warning(LogCategory.Core,
                    $"[HubSceneEntrypoint] No assembly at Resources/{NpcAssemblyResourcePath}; " +
                    "the keeper is label-only.");
            }

            var label = CreateOverhead(root.transform,
                root.transform.position + Vector3.up * OverheadLabelHeight,
                _config.NpcDisplayName, TalkPromptVerb);
            _proximity.Register(
                new HubInteractionSpot("keeper", root.transform.position.x, root.transform.position.z,
                    _config.NpcInteractRadius),
                _staging.ShowOffer,
                label);
        }

        /// <summary>
        /// The Heat pact spot (Track Y): a squat ember-tinted pot mirroring the keeper on
        /// screen-right — the dare comes from the cauldron itself, not the junk-keeper. Skipped
        /// entirely when no pact can be offered (no menu authored / no shared panel): a hub
        /// without Heat looks exactly as before.
        /// </summary>
        private void PlaceCauldron(HubPlatform platform, Vector3 center)
        {
            if (_heatPact == null || !_heatPact.HasMenu)
            {
                return;
            }

            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "HubCauldron";
            pot.transform.SetParent(platform.GameObject.transform);
            var position = SnapToFreeCell(platform,
                center - ScreenLeft * CauldronScreenRightDistance);
            pot.transform.position = position + Vector3.up * CauldronScale.y;
            pot.transform.localScale = CauldronScale;

            // Interaction is distance-based; the primitive's collider would only trip the hero.
            var collider = pot.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = pot.GetComponent<MeshRenderer>();
            if (renderer != null && _heatStyle != null)
            {
                renderer.material.color = _heatStyle.PactCardTint;
            }

            // Parented to the UNscaled platform root (the portal-label precedent).
            var label = CreateOverhead(platform.GameObject.transform,
                position + Vector3.up * OverheadLabelHeight,
                CauldronDisplayName, DarePromptVerb);
            _proximity.Register(
                new HubInteractionSpot("cauldron", position.x, position.z, _config.NpcInteractRadius),
                _heatPact.ShowPact,
                label);
        }

        private void PlacePortals(HubPlatform platform, Vector3 center, float extent)
        {
            float ringRadius = Mathf.Max(MinPortalRingRadius, extent * PortalRingFraction);
            var homelands = _staging.Homelands;
            for (int i = 0; i < homelands.Count; i++)
            {
                var homeland = homelands[i];
                float angle = (PortalArcStartDegrees + PortalArcStepDegrees * i) * Mathf.Deg2Rad;
                var position = SnapToFreeCell(platform, center + new Vector3(
                    Mathf.Sin(angle) * ringRadius, 0f, Mathf.Cos(angle) * ringRadius));

                BuildPortalDisc(platform, homeland, position);
                // Parented to the UNscaled platform root — a child of the squashed disc would
                // inherit its Y scale and flatten the text.
                var label = CreateOverhead(platform.GameObject.transform,
                    position + Vector3.up * OverheadLabelHeight,
                    homeland.Label, homeland.PromptName);

                var captured = homeland;
                _proximity.Register(
                    new HubInteractionSpot($"portal:{homeland.Theme}", position.x, position.z,
                        _config.PortalInteractRadius),
                    () => _staging.LaunchInto(captured.Theme),
                    label);
            }

            _logger.Info(LogCategory.Core,
                $"[HubSceneEntrypoint] Hub assembled: keeper + {homelands.Count} portal(s), ring {ringRadius:F1}.");
        }

        private static GameObject BuildPortalDisc(HubPlatform platform, HubHomeland homeland, Vector3 position)
        {
            var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = $"Portal_{homeland.Theme}";
            portal.transform.SetParent(platform.GameObject.transform);
            portal.transform.position = position + Vector3.up * PortalDiscScale.y;
            portal.transform.localScale = PortalDiscScale;

            // Interaction is distance-based; the primitive's collider would only trip the hero.
            var collider = portal.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = portal.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = PortalTint(homeland.Theme);
            }

            return portal;
        }

        private static Color PortalTint(LevelGeneration.LevelTheme theme)
        {
            switch (theme)
            {
                case LevelGeneration.LevelTheme.Forest: return new Color(0.28f, 0.52f, 0.3f);
                case LevelGeneration.LevelTheme.Desert: return new Color(0.76f, 0.66f, 0.4f);
                case LevelGeneration.LevelTheme.Mountain: return new Color(0.55f, 0.58f, 0.66f);
                default: return Color.white;
            }
        }

        private HubOverheadLabelView CreateOverhead(
            Transform parent, Vector3 worldPosition, string displayName, string promptVerb)
        {
            var overhead = new GameObject("Overhead");
            overhead.transform.SetParent(parent);
            overhead.transform.position = worldPosition;

            var view = overhead.AddComponent<HubOverheadLabelView>();
            view.SetName(displayName);
            view.SetPrompt(ComposePrompt(promptVerb));
            _promptedLabels.Add((view, promptVerb));
            return view;
        }

        /// <summary>Device switch mid-session: re-render every hub prompt with the new source's cue.</summary>
        private void RefreshPrompts()
        {
            for (int i = 0; i < _promptedLabels.Count; i++)
            {
                var (view, verb) = _promptedLabels[i];
                if (view != null)
                {
                    view.SetPrompt(ComposePrompt(verb));
                }
            }
        }

        private string ComposePrompt(string verb)
        {
            return $"[{_cues.GetCue(GameAction.Interact)}] {verb}";
        }

        /// <summary>
        /// Nearest walkable cell centre to <paramref name="desired"/> that is neither blocked by a
        /// dressing feature nor already claimed by an earlier spot; the chosen cell is then reserved.
        /// This is what keeps the three portals (and the keeper) on distinct cells — snapping each
        /// independently to the plain nearest cell collapsed adjacent portals into one. Falls back
        /// to the plain nearest cell only if every cell is blocked/claimed (a degenerate island).
        /// </summary>
        private Vector3 SnapToFreeCell(HubPlatform platform, Vector3 desired)
        {
            var surface = platform.Surface;
            var origin = platform.GameObject.transform.position;

            bool found = false;
            HexCoordinates best = default;
            float bestSq = float.MaxValue;
            foreach (var cell in surface.Cells)
            {
                if (surface.IsBlocked(cell) || _claimedCells.Contains(cell))
                {
                    continue;
                }

                var (x, z) = surface.GetCellCenterLocal(cell);
                float dx = origin.x + x - desired.x;
                float dz = origin.z + z - desired.z;
                float distSq = dx * dx + dz * dz;
                if (distSq < bestSq)
                {
                    bestSq = distSq;
                    best = cell;
                    found = true;
                }
            }

            if (!found)
            {
                return global::Platform.PlatformAnchor.NearestCellWorld(surface, origin, desired);
            }

            _claimedCells.Add(best);
            var (bx, bz) = surface.GetCellCenterLocal(best);
            return origin + new Vector3(bx, 0f, bz);
        }

        private static float PlanarExtent(HubPlatform platform, Vector3 center)
        {
            float maxSq = 0f;
            foreach (var point in platform.Surface.Outline)
            {
                float dx = point.X - center.x;
                float dz = point.Z - center.z;
                float distSq = dx * dx + dz * dz;
                if (distSq > maxSq)
                {
                    maxSq = distSq;
                }
            }

            return Mathf.Sqrt(maxSq);
        }
    }
}
