using Core.Logging;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
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
    public sealed class HubSceneEntrypoint : IInitializable
    {
        private const string NpcAssemblyResourcePath = "CharacterSystem/Assemblies/PlaceholderAssembly_A";
        private const string TalkPromptLabel = "[F] Talk";

        /// <summary>The keeper stands just left of centre on screen (camera looks from +Z).</summary>
        private static readonly Vector3 NpcOffsetFromCenter = new Vector3(1.8f, 0f, 0.4f);

        private const float OverheadLabelHeight = 2.2f;
        /// <summary>Portals sit on the far arc, framed above the centre from the camera.</summary>
        private const float PortalArcStartDegrees = 150f;
        private const float PortalArcStepDegrees = 30f;
        private const float PortalRingFraction = 0.6f;
        private const float MinPortalRingRadius = 2.5f;

        private static readonly Vector3 PortalDiscScale = new Vector3(1.5f, 0.05f, 1.5f);

        private readonly HubPlatformBuilder _platformBuilder;
        private readonly Data.HubSceneConfig _config;
        private readonly IModularCharacterFactory _characterFactory;
        private readonly HubStagingPresenter _staging;
        private readonly HubProximityPresenter _proximity;
        private readonly IGameLogger _logger;

        public HubSceneEntrypoint(
            HubPlatformBuilder platformBuilder,
            Data.HubSceneConfig config,
            IModularCharacterFactory characterFactory,
            HubStagingPresenter staging,
            HubProximityPresenter proximity,
            IGameLogger logger)
        {
            _platformBuilder = platformBuilder;
            _config = config;
            _characterFactory = characterFactory;
            _staging = staging;
            _proximity = proximity;
            _logger = logger;
        }

        public void Initialize()
        {
            var platform = _platformBuilder.Build();
            var center = global::Platform.PlatformAnchor.CenterCellWorld(
                platform.Surface, platform.GameObject.transform.position);
            float extent = PlanarExtent(platform, center);

            PlaceKeeper(platform, center);
            PlacePortals(platform, center, extent);
        }

        private void PlaceKeeper(HubPlatform platform, Vector3 center)
        {
            var root = new GameObject("HubKeeper");
            root.transform.SetParent(platform.GameObject.transform);
            root.transform.position = center + NpcOffsetFromCenter;

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
                _config.NpcDisplayName, TalkPromptLabel);
            _proximity.Register(
                new HubInteractionSpot("keeper", root.transform.position.x, root.transform.position.z,
                    _config.NpcInteractRadius),
                _staging.ShowOffer,
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
                var position = center + new Vector3(
                    Mathf.Sin(angle) * ringRadius, 0f, Mathf.Cos(angle) * ringRadius);

                BuildPortalDisc(platform, homeland, position);
                // Parented to the UNscaled platform root — a child of the squashed disc would
                // inherit its Y scale and flatten the text.
                var label = CreateOverhead(platform.GameObject.transform,
                    position + Vector3.up * OverheadLabelHeight,
                    homeland.Label, $"[F] {homeland.PromptName}");

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
                Object.Destroy(collider);
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

        private static HubOverheadLabelView CreateOverhead(
            Transform parent, Vector3 worldPosition, string displayName, string prompt)
        {
            var overhead = new GameObject("Overhead");
            overhead.transform.SetParent(parent);
            overhead.transform.position = worldPosition;

            var view = overhead.AddComponent<HubOverheadLabelView>();
            view.SetName(displayName);
            view.SetPrompt(prompt);
            return view;
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
