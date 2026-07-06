using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Runtime;
using Combat.View;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace UI.AbilityPreview
{
    /// <summary>
    /// The shared popover's 3D stage (crib of the mutation preview rig): a hidden hero clone on
    /// a mock ground at a far world offset, one camera → RenderTexture. The cast demonstration
    /// loops while showing — the animator trigger fires (only if the controller actually has
    /// the parameter; placeholder rigs mostly do not, then the hero idles) and the D3 cell
    /// sweep plays over the ability's shape. A passive shows the idle hero only. The clone is
    /// rebuilt per request from the scene's <see cref="IAbilityPreviewHeroSource"/>.
    /// </summary>
    public class AbilityPreviewStageRig : MonoBehaviour, IAbilityPreviewStage
    {
        private const int DepthBufferBits = 16;

        [Inject] private IModularCharacterFactory _factory;
        [Inject] private IAbilityPreviewHeroSource _heroSource;
        [Inject] private AbilityPreviewConfig _config;
        [Inject] private IGameLogger _logger;

        private Transform _modelAnchor;
        private Transform _sweepAnchor;
        private Camera _camera;
        private RenderTexture _texture;
        private ModularCharacter _previewCharacter;
        private Coroutine _castLoop;
        private readonly List<GameObject> _activeFlashes = new List<GameObject>();

        public bool TryShow(AbilityPreviewData data, out Texture texture)
        {
            texture = null;
            if (!_heroSource.TryGetHero(out var assembly, out var partOverrides) || assembly == null)
            {
                // No hero to demonstrate with — the popover degrades to text-only.
                return false;
            }

            EnsureRig();
            StopCastLoop();
            DestroyPreviewCharacter();

            var preview = _factory.Create(assembly, _modelAnchor);
            if (preview == null)
            {
                _logger?.Warning(LogCategory.UI,
                    "[AbilityPreviewStageRig] Failed to assemble the preview hero.");
                return false;
            }

            var previewTransform = preview.transform;
            previewTransform.localPosition = Vector3.zero;
            previewTransform.localRotation = Quaternion.Euler(0f, _config.ModelYawDegrees, 0f);
            _previewCharacter = preview;

            if (partOverrides != null)
            {
                foreach (var slotToPart in partOverrides)
                {
                    preview.SwapPart(slotToPart.Key, slotToPart.Value);
                }
            }

            _camera.enabled = true;
            _castLoop = StartCoroutine(CastLoop(data));
            texture = _texture;
            return true;
        }

        public void Hide()
        {
            StopCastLoop();
            DestroyPreviewCharacter();
            if (_camera != null)
            {
                _camera.enabled = false;
            }
        }

        private IEnumerator CastLoop(AbilityPreviewData data)
        {
            var animator = _previewCharacter != null
                ? _previewCharacter.GetComponentInChildren<Animator>()
                : null;
            bool hasTrigger = AnimatorHasTrigger(animator, data.AnimationTrigger);

            while (true)
            {
                if (hasTrigger)
                {
                    animator.SetTrigger(data.AnimationTrigger);
                }

                if (!data.IsPassive)
                {
                    ClearFlashes();
                    var cells = AbilityPreviewShape.CellPositions(
                        data.IsLine, data.LineLength, data.RingRadius, _config.CellSize);
                    for (int i = 0; i < cells.Count; i++)
                    {
                        cells[i] = _sweepAnchor.TransformPoint(cells[i]);
                    }

                    _activeFlashes.AddRange(AbilityAreaSweep.Play(
                        _sweepAnchor,
                        cells,
                        _sweepAnchor.position,
                        data.IsLine,
                        _config.SweepTint,
                        _config.SweepPeakAlpha,
                        _config.SweepSeconds,
                        _config.CellSize));
                }

                yield return new WaitForSeconds(_config.LoopIntervalSeconds);
            }
        }

        private static bool AnimatorHasTrigger(Animator animator, string trigger)
        {
            if (animator == null || string.IsNullOrEmpty(trigger) || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == trigger)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureRig()
        {
            if (_modelAnchor != null)
            {
                return;
            }

            transform.position = _config.RigWorldOffset;

            _modelAnchor = new GameObject("ModelAnchor").transform;
            _modelAnchor.SetParent(transform, worldPositionStays: false);

            // The sweep spawns in front of the hero, on the ground plane.
            _sweepAnchor = new GameObject("SweepAnchor").transform;
            _sweepAnchor.SetParent(transform, worldPositionStays: false);
            _sweepAnchor.localRotation = Quaternion.Euler(0f, _config.ModelYawDegrees + 180f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "MockGround";
            Destroy(ground.GetComponent<Collider>());
            ground.transform.SetParent(transform, worldPositionStays: false);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = Vector3.one * _config.GroundSize;
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.material.color = _config.GroundColor;

            _texture = new RenderTexture(_config.TextureSize, _config.TextureSize, DepthBufferBits);

            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(transform, worldPositionStays: false);
            cameraObject.transform.localPosition = new Vector3(0f, _config.CameraHeight, _config.CameraDistance);
            cameraObject.transform.LookAt(_modelAnchor.position + Vector3.up * _config.LookAtHeight);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = _config.BackgroundColor;
            _camera.fieldOfView = _config.FieldOfView;
            _camera.farClipPlane = _config.FarClipPlane;
            _camera.enabled = false;

            var lightObject = new GameObject("PreviewLight");
            lightObject.transform.SetParent(transform, worldPositionStays: false);
            lightObject.transform.localPosition = _config.LightLocalPosition;
            var previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Point;
            previewLight.range = _config.LightRange;
            previewLight.intensity = _config.LightIntensity;
        }

        private void StopCastLoop()
        {
            if (_castLoop != null)
            {
                StopCoroutine(_castLoop);
                _castLoop = null;
            }

            ClearFlashes();
        }

        private void ClearFlashes()
        {
            foreach (var flash in _activeFlashes)
            {
                if (flash != null)
                {
                    Destroy(flash);
                }
            }

            _activeFlashes.Clear();
        }

        private void DestroyPreviewCharacter()
        {
            if (_previewCharacter != null)
            {
                Destroy(_previewCharacter.gameObject);
                _previewCharacter = null;
            }
        }

        private void OnDestroy()
        {
            StopCastLoop();
            if (_texture != null)
            {
                _texture.Release();
                _texture = null;
            }
        }
    }
}
