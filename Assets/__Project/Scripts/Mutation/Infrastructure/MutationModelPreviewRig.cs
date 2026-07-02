using CharacterSystem.Runtime;
using Core.Logging;
using Mutation.Data.Definitions;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Mutation.Infrastructure
{
    /// <summary>
    /// Runtime implementation of <see cref="IMutationModelPreview"/>: keeps a hidden copy
    /// of the modular hero at a far world offset (so no gameplay camera ever sees it),
    /// swaps the offered part in, and renders it with a dedicated camera into a
    /// RenderTexture the popover shows. The clone is rebuilt per request (no stale swap
    /// state) and destroyed on hide; the camera only renders while a preview is showing.
    /// All geometry/light tunables live in <see cref="MutationPreviewSettings"/>.
    /// </summary>
    public class MutationModelPreviewRig : MonoBehaviour, IMutationModelPreview
    {
        private const int DepthBufferBits = 16;

        [Inject] private IModularCharacterFactory _factory;
        [Inject] private ModularCharacterVisual _heroVisual;
        [Inject] private MutationConfig _config;
        [Inject] private IGameLogger _logger;

        private Transform _modelAnchor;
        private Camera _camera;
        private RenderTexture _texture;
        private ModularCharacter _previewCharacter;

        public bool TryShow(string slotId, string partId, out Texture texture)
        {
            texture = null;
            if (_heroVisual == null || _heroVisual.Character == null)
            {
                // The hero rig is not assembled yet; the popover just does not open.
                return false;
            }

            EnsureRig();
            DestroyPreviewCharacter();

            var preview = _factory.Create(_heroVisual.Assembly, _modelAnchor);
            if (preview == null)
            {
                _logger?.Warning(LogCategory.Mutation,
                    "[MutationModelPreviewRig] Failed to assemble the preview character.");
                return false;
            }

            var settings = _config.Preview;
            var previewTransform = preview.transform;
            previewTransform.localPosition = Vector3.zero;
            previewTransform.localRotation = Quaternion.Euler(0f, settings.ModelYawDegrees, 0f);
            _previewCharacter = preview;

            // Mirror the live hero's mutations, then apply the offered part on top so the
            // silhouette shows exactly what the player would become.
            foreach (var equipped in _heroVisual.Character.EquippedParts)
            {
                preview.SwapPart(equipped.Key, equipped.Value);
            }

            if (!preview.SwapPart(slotId, partId))
            {
                _logger?.Warning(LogCategory.Mutation,
                    $"[MutationModelPreviewRig] Preview swap failed for part '{partId}' in slot " +
                    $"'{slotId}'; showing the current silhouette instead.");
            }

            _camera.enabled = true;
            texture = _texture;
            return true;
        }

        public void Hide()
        {
            DestroyPreviewCharacter();
            if (_camera != null)
            {
                _camera.enabled = false;
            }
        }

        private void EnsureRig()
        {
            if (_modelAnchor != null)
            {
                return;
            }

            var settings = _config.Preview;
            transform.position = settings.RigWorldOffset;

            _modelAnchor = new GameObject("ModelAnchor").transform;
            _modelAnchor.SetParent(transform, worldPositionStays: false);

            _texture = new RenderTexture(settings.TextureSize, settings.TextureSize, DepthBufferBits);

            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(transform, worldPositionStays: false);
            cameraObject.transform.localPosition =
                new Vector3(0f, settings.CameraHeight, settings.CameraDistance);
            cameraObject.transform.LookAt(
                _modelAnchor.position + Vector3.up * settings.LookAtHeight);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = settings.BackgroundColor;
            _camera.fieldOfView = settings.FieldOfView;
            _camera.farClipPlane = settings.FarClipPlane;
            _camera.enabled = false;

            var lightObject = new GameObject("PreviewLight");
            lightObject.transform.SetParent(transform, worldPositionStays: false);
            lightObject.transform.localPosition = settings.LightLocalPosition;
            var previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Point;
            previewLight.range = settings.LightRange;
            previewLight.intensity = settings.LightIntensity;
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
            if (_texture != null)
            {
                _texture.Release();
                _texture = null;
            }
        }
    }
}
