using System.Linq;
using Combat.Battlefield;
using Core.Logging;
using LevelGeneration;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using UnityEngine;
using World.Dressing.Core;
using World.Sites.Core;

namespace Hub.View
{
    /// <summary>
    /// Assembles the Hub's one island THROUGH the normal world-platform path (O1 rework,
    /// hub-as-a-normal-platform.md): the shared shape profile grows the hex surface from the
    /// config's fixed seed, the environment-dressing planner dresses it as a wild platform of the
    /// **Hub biome** (blockers folded into the surface before the mesh, ground material resolved
    /// and toned from the authored hub kit), and <see cref="global::Platform.PlatformView"/> —
    /// the world platform's own visual — builds the mesh, walkable colliders, and perimeter
    /// walls. Pure orchestration: zero platform logic lives here.
    /// </summary>
    public class HubPlatformAssembler
    {
        /// <summary>The Hub's single platform is node 0 of its one-platform world (seed context).</summary>
        private const int HubNodeId = 0;

        private static readonly Color FallbackJunkyardTint = new Color(0.42f, 0.36f, 0.3f);

        private readonly PlatformShapeSettings _shapeSettings;
        private readonly Data.HubSceneConfig _config;
        private readonly IEnvironmentDressingPlanner _dressingPlanner;
        private readonly IEnvironmentDressingSpawner _dressingSpawner;
        private readonly IGameLogger _logger;

        public HubPlatformAssembler(
            PlatformShapeSettings shapeSettings,
            Data.HubSceneConfig config,
            IEnvironmentDressingPlanner dressingPlanner,
            IEnvironmentDressingSpawner dressingSpawner,
            IGameLogger logger)
        {
            _shapeSettings = shapeSettings;
            _config = config;
            _dressingPlanner = dressingPlanner;
            _dressingSpawner = dressingSpawner;
            _logger = logger;
        }

        public HubPlatform Build()
        {
            var rng = new DeterministicRandom(unchecked((ulong)(uint)_config.PlatformSeed));
            var surface = new PlatformSurfaceGenerator().Generate(
                _shapeSettings.Combat,
                _shapeSettings.BattlefieldMinimumCells,
                _shapeSettings.HexSize,
                _shapeSettings.Orientation,
                _shapeSettings.RimWidth,
                _shapeSettings.RimJitterPercent,
                rng);

            // Dress as a WILD platform of the Hub biome (the theme provider is pre-set to Hub):
            // blockers must fold into the surface BEFORE the mesh/colliders consume it — exactly
            // the AreaGenerator order.
            var plan = _dressingPlanner.Plan(
                HubNodeId, SiteStamp.Wild, PlatformContentKind.Empty, surface,
                _shapeSettings.BattlefieldMinimumCells);
            if (plan.BlockedCells.Count > 0)
            {
                surface = surface.WithBlockedCells(plan.BlockedCells);
            }

            var root = new GameObject("HubPlatform");
            root.transform.position = Vector3.zero;

            var view = root.AddComponent<global::Platform.PlatformView>();
            Material ground = _dressingSpawner.ResolveGroundMaterial(plan);
            view.SetConfig(
                ground != null
                    ? ground
                    : new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = FallbackJunkyardTint },
                _shapeSettings.PlatformThickness,
                _shapeSettings.RimDropHeight,
                _shapeSettings.CellInset);
            view.Initialize(surface, surface.Outline.Select(p => new Vector3(p.X, 0f, p.Z)).ToList());

            _dressingSpawner.Spawn(plan, root.transform);

            _logger.Info(LogCategory.Core,
                $"[HubPlatformAssembler] Hub island built as a normal platform: {surface.Cells.Count} cells " +
                $"(seed {_config.PlatformSeed}, ground {(ground != null ? "hub kit" : "code fallback")}).");

            return new HubPlatform(surface, root);
        }
    }

    /// <summary>The built Hub island: the surface truth and its scene object.</summary>
    public class HubPlatform
    {
        public PlatformHexSurface Surface { get; }
        public GameObject GameObject { get; }

        public HubPlatform(PlatformHexSurface surface, GameObject gameObject)
        {
            Surface = surface;
            GameObject = gameObject;
        }
    }
}
