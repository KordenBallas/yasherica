using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Data;
using CharacterSystem.Runtime;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Arena.View
{
    /// <summary>
    /// The draft screen's procedural 3D stage (crib of the preview-rig pattern): part display
    /// models on a pedestal grid GROUPED BY SLOT (one column per loadout slot — G4 req 1),
    /// the local monster assembling live beside the board (req 8), one camera rendering it
    /// all to a texture the uGUI viewport shows. Clicks arrive as viewport UVs and resolve
    /// against the pedestals' colliders; hero-part clicks resolve against per-part colliders
    /// fitted after every body update. Far world offset — no gameplay camera ever sees it.
    /// </summary>
    public class ArenaDraftStageRig : MonoBehaviour, IArenaDraftStage
    {
        private const int DepthBufferBits = 16;
        private const int TextureWidth = 1024;
        private const int TextureHeight = 768;
        private const float PedestalSize = 0.55f;
        private const float ColumnSpacing = 1.1f;
        private const float RowSpacing = 0.9f;
        private const float HeroOffsetX = -2.6f;
        private const float CameraHeight = 3.2f;
        private const float CameraDistance = 6.5f;
        private const float CameraFieldOfView = 42f;
        private static readonly Vector3 RigWorldOffset = new Vector3(-4000f, 0f, 4000f);
        private static readonly Color BackgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f);

        [Inject] private IModularCharacterFactory _factory;
        [Inject] private IPartCatalog _partCatalog;
        [Inject] private ArenaDraftConfig _draftConfig;
        [Inject] private IGameLogger _logger;

        private readonly Dictionary<int, GameObject> _pedestalsByEntryId = new Dictionary<int, GameObject>();
        private readonly Dictionary<Collider, int> _entryIdByCollider = new Dictionary<Collider, int>();
        private readonly Dictionary<Collider, string> _heroSlotByCollider = new Dictionary<Collider, string>();

        private Transform _boardAnchor;
        private Transform _heroAnchor;
        private Camera _camera;
        private RenderTexture _texture;
        private ModularCharacter _heroClone;

        public Texture Texture => _texture;

        public void Build(IReadOnlyList<ArenaDraftBoardEntry> board, IReadOnlyList<string> slotLoadout)
        {
            EnsureRig();
            Clear();

            // One column per loadout slot, entries stacked away from the camera.
            var slotColumns = new Dictionary<string, int>();
            for (int i = 0; i < slotLoadout.Count; i++)
            {
                slotColumns[slotLoadout[i]] = i;
            }

            var rowsPerColumn = new Dictionary<int, int>();
            float columnsCenter = (slotLoadout.Count - 1) * 0.5f;
            foreach (var entry in board)
            {
                if (!slotColumns.TryGetValue(entry.SlotId, out var column))
                {
                    continue;
                }

                if (!_partCatalog.TryGet(entry.PartId, out var part))
                {
                    _logger.Warning(LogCategory.Combat,
                        $"[ArenaDraftStageRig] Board entry {entry.EntryId} part '{entry.PartId}' " +
                        "is not in the part catalog — pedestal skipped.");
                    continue;
                }

                rowsPerColumn.TryGetValue(column, out var row);
                rowsPerColumn[column] = row + 1;

                var pedestal = DraftPartModelFactory.CreateDisplayModel(part, _boardAnchor, PedestalSize);
                pedestal.transform.localPosition = new Vector3(
                    (column - columnsCenter) * ColumnSpacing, 0f, row * RowSpacing);

                _pedestalsByEntryId[entry.EntryId] = pedestal;
                foreach (var pedestalCollider in pedestal.GetComponentsInChildren<Collider>())
                {
                    _entryIdByCollider[pedestalCollider] = entry.EntryId;
                }
            }

            UpdateLocalHero(new Dictionary<string, string>());
            _camera.enabled = true;
        }

        public void UpdateLocalHero(IReadOnlyDictionary<string, string> loadout)
        {
            EnsureRig();
            DestroyHeroClone();

            if (_draftConfig.BaseAssembly == null)
            {
                _logger.Warning(LogCategory.Combat,
                    "[ArenaDraftStageRig] ArenaDraftConfig has no base assembly — no live hero on the stage.");
                return;
            }

            _heroClone = _factory.Create(_draftConfig.BaseAssembly, _heroAnchor);
            if (_heroClone == null)
            {
                return;
            }

            _heroClone.transform.localPosition = Vector3.zero;
            _heroClone.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);

            if (loadout != null)
            {
                foreach (var slotToPart in loadout)
                {
                    _heroClone.SwapPart(slotToPart.Key, slotToPart.Value);
                }
            }

            FitHeroPartColliders(loadout);
        }

        public void RemoveEntry(int entryId)
        {
            if (!_pedestalsByEntryId.TryGetValue(entryId, out var pedestal))
            {
                return;
            }

            foreach (var pedestalCollider in pedestal.GetComponentsInChildren<Collider>())
            {
                _entryIdByCollider.Remove(pedestalCollider);
            }

            _pedestalsByEntryId.Remove(entryId);
            Destroy(pedestal);
        }

        public bool TryRaycast(Vector2 viewportUv, out ArenaDraftStageHit hit)
        {
            hit = default;
            if (_camera == null)
            {
                return false;
            }

            var ray = _camera.ViewportPointToRay(new Vector3(viewportUv.x, viewportUv.y, 0f));
            if (!Physics.Raycast(ray, out var raycastHit, _camera.farClipPlane))
            {
                return false;
            }

            if (_entryIdByCollider.TryGetValue(raycastHit.collider, out var entryId))
            {
                hit = ArenaDraftStageHit.Entry(entryId);
                return true;
            }

            if (_heroSlotByCollider.TryGetValue(raycastHit.collider, out var slotId))
            {
                hit = ArenaDraftStageHit.HeroPart(slotId);
                return true;
            }

            return false;
        }

        public bool TryGetEntryViewportPoint(int entryId, out Vector2 viewportUv)
        {
            viewportUv = default;
            if (_camera == null || !_pedestalsByEntryId.TryGetValue(entryId, out var pedestal))
            {
                return false;
            }

            var point = _camera.WorldToViewportPoint(pedestal.transform.position);
            viewportUv = new Vector2(point.x, point.y);
            return point.z > 0f;
        }

        public void Clear()
        {
            foreach (var pedestal in _pedestalsByEntryId.Values)
            {
                if (pedestal != null)
                {
                    Destroy(pedestal);
                }
            }

            _pedestalsByEntryId.Clear();
            _entryIdByCollider.Clear();
            _heroSlotByCollider.Clear();
            DestroyHeroClone();
            if (_camera != null)
            {
                _camera.enabled = false;
            }
        }

        /// <summary>
        /// Per-part click targets on the assembling hero (req 11): a box around each skinned
        /// part renderer, mapped back to its slot by matching renderer names against the
        /// drafted parts' prefab names. A part the heuristic cannot match just is not
        /// clickable on the hero — the dual text readout stays the reliable info path.
        /// </summary>
        private void FitHeroPartColliders(IReadOnlyDictionary<string, string> loadout)
        {
            _heroSlotByCollider.Clear();
            if (_heroClone == null || loadout == null || loadout.Count == 0)
            {
                return;
            }

            var slotByPrefabName = new Dictionary<string, string>();
            foreach (var slotToPart in loadout)
            {
                if (_partCatalog.TryGet(slotToPart.Value, out var part) && part.PartPrefab != null)
                {
                    slotByPrefabName[part.PartPrefab.name] = slotToPart.Key;
                }
            }

            foreach (var renderer in _heroClone.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var match = slotByPrefabName.FirstOrDefault(
                    pair => renderer.gameObject.name.StartsWith(pair.Key));
                if (string.IsNullOrEmpty(match.Value))
                {
                    continue;
                }

                var partCollider = renderer.gameObject.AddComponent<BoxCollider>();
                var localBounds = renderer.localBounds;
                partCollider.center = localBounds.center;
                partCollider.size = localBounds.size;
                _heroSlotByCollider[partCollider] = match.Value;
            }
        }

        private void EnsureRig()
        {
            if (_boardAnchor != null)
            {
                return;
            }

            transform.position = RigWorldOffset;

            _boardAnchor = new GameObject("BoardAnchor").transform;
            _boardAnchor.SetParent(transform, worldPositionStays: false);

            _heroAnchor = new GameObject("HeroAnchor").transform;
            _heroAnchor.SetParent(transform, worldPositionStays: false);
            _heroAnchor.localPosition = new Vector3(HeroOffsetX, 0f, 1.2f);

            _texture = new RenderTexture(TextureWidth, TextureHeight, DepthBufferBits);

            var cameraObject = new GameObject("StageCamera");
            cameraObject.transform.SetParent(transform, worldPositionStays: false);
            cameraObject.transform.localPosition = new Vector3(0f, CameraHeight, -CameraDistance);
            cameraObject.transform.LookAt(transform.position + Vector3.forward * 1.5f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BackgroundColor;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.farClipPlane = 60f;
            _camera.enabled = false;

            var lightObject = new GameObject("StageLight");
            lightObject.transform.SetParent(transform, worldPositionStays: false);
            lightObject.transform.localPosition = new Vector3(2f, 4f, -2f);
            var stageLight = lightObject.AddComponent<Light>();
            stageLight.type = LightType.Point;
            stageLight.range = 25f;
            stageLight.intensity = 1.5f;
        }

        private void DestroyHeroClone()
        {
            if (_heroClone != null)
            {
                Destroy(_heroClone.gameObject);
                _heroClone = null;
            }
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                _texture = null;
            }
        }
    }
}
