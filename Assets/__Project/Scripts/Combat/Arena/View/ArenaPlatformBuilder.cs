using System.Linq;
using Combat.Arena.Data;
using Combat.Battlefield;
using Core.Logging;
using LevelGeneration.Surface;
using Loot.Core;
using Narrative.Director.Core;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>
    /// Builds the one arena platform standalone (no streaming world generator): the hex surface
    /// grows from the match seed with the combat shape profile + battlefield minimum, so every
    /// client derives the identical surface — the combat grid, mesh, and colliders all read it as
    /// the single source of truth, exactly like a world platform.
    /// </summary>
    public class ArenaPlatformBuilder
    {
        private const string PlatformSeedContext = "arena-platform";

        private readonly PlatformShapeSettings _shapeSettings;
        private readonly ArenaMatchConfig _config;
        private readonly IGameLogger _logger;

        public ArenaPlatformBuilder(
            PlatformShapeSettings shapeSettings,
            ArenaMatchConfig config,
            IGameLogger logger)
        {
            _shapeSettings = shapeSettings;
            _config = config;
            _logger = logger;
        }

        public ArenaPlatform Build(int matchSeed)
        {
            var seed = LootSeed.Derive(matchSeed, PlatformSeedContext);
            var rng = new DeterministicRandom(unchecked((ulong)(uint)seed));

            var surface = new PlatformSurfaceGenerator().Generate(
                _shapeSettings.Combat,
                _shapeSettings.BattlefieldMinimumCells,
                _shapeSettings.HexSize,
                _shapeSettings.Orientation,
                _shapeSettings.RimWidth,
                _shapeSettings.RimJitterPercent,
                rng);

            var root = new GameObject("ArenaPlatform");
            root.transform.position = Vector3.zero;

            var meshObject = new GameObject("PlatformMesh");
            meshObject.transform.SetParent(root.transform);
            meshObject.transform.localPosition = Vector3.zero;

            var mesh = global::Platform.PlatformHexSurfaceMeshBuilder.Build(
                surface,
                _shapeSettings.PlatformThickness,
                _shapeSettings.RimDropHeight,
                _shapeSettings.CellInset);

            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _config.PlatformMaterial != null
                ? _config.PlatformMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.42f, 0.38f) };

            meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Walls follow the stitched walkable outline — the rim stays physically unreachable.
            global::Platform.PlatformColliderBuilder.BuildPlatformColliders(
                root,
                surface.Outline.Select(p => new Vector3(p.X, 0f, p.Z)).ToList(),
                _shapeSettings.PlatformThickness);

            _logger.Info(LogCategory.Combat,
                $"[ArenaPlatformBuilder] Built arena platform: {surface.Cells.Count} cells (seed {matchSeed})");

            return new ArenaPlatform(surface, root);
        }
    }

    /// <summary>The built arena stage: the shared-truth surface and its scene object.</summary>
    public class ArenaPlatform
    {
        public PlatformHexSurface Surface { get; }
        public GameObject GameObject { get; }
        public Vector3 Position => GameObject.transform.position;

        public ArenaPlatform(PlatformHexSurface surface, GameObject gameObject)
        {
            Surface = surface;
            GameObject = gameObject;
        }
    }
}
