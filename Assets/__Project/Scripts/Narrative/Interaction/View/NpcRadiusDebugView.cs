using System.Collections.Generic;
using Narrative.Interaction.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Narrative.Interaction.View
{
    /// <summary>
    /// Dev-only overlay (R14): toggled with F2, it draws each NPC's interaction or aggro radius as a ground
    /// ring (LineRenderer, so it shows in the Game view) in two distinct colors, so the global distances in
    /// <see cref="NpcInteractionSettings"/> can be tuned by eye. Not shipped player UX — installed only in
    /// editor/development builds.
    /// </summary>
    public sealed class NpcRadiusDebugView : MonoBehaviour
    {
        private const Key ToggleKey = Key.F2;
        private const int Segments = 48;
        private const float LineWidth = 0.06f;

        private static readonly Color InteractionColor = new Color(0.3f, 0.8f, 1f, 0.9f);
        private static readonly Color AggroColor = new Color(1f, 0.35f, 0.25f, 0.9f);

        private INpcInteractionRegistry _registry;
        private NpcInteractionSettings _settings;

        private readonly Dictionary<string, LineRenderer> _rings = new Dictionary<string, LineRenderer>();
        private readonly HashSet<string> _seen = new HashSet<string>();
        private Material _ringMaterial;
        private bool _enabled;

        [Inject]
        public void Construct(INpcInteractionRegistry registry, NpcInteractionSettings settings)
        {
            _registry = registry;
            _settings = settings;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame)
            {
                _enabled = !_enabled;
                if (!_enabled)
                {
                    HideAll();
                }
            }

            if (!_enabled || _registry == null || _settings == null)
            {
                return;
            }

            _seen.Clear();
            var handles = _registry.Handles;
            for (int i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                if (handle.PositionSource == null)
                {
                    continue;
                }

                bool hostile = handle.Intent == NpcIntent.Hostile;
                float radius = hostile ? _settings.AggroRadius : _settings.InteractionRadius;
                Color color = hostile ? AggroColor : InteractionColor;

                var ring = GetRing(handle.Id);
                DrawCircle(ring, handle.PositionSource.position, radius, color);
                _seen.Add(handle.Id);
            }

            HideUnseen();
        }

        private LineRenderer GetRing(string id)
        {
            if (_rings.TryGetValue(id, out var ring) && ring != null)
            {
                ring.enabled = true;
                return ring;
            }

            var go = new GameObject($"NpcRadiusRing_{id}");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.material = RingMaterial();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = Segments;
            ring.widthMultiplier = LineWidth;
            ring.numCapVertices = 0;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            _rings[id] = ring;
            return ring;
        }

        private Material RingMaterial()
        {
            if (_ringMaterial == null)
            {
                _ringMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return _ringMaterial;
        }

        private static void DrawCircle(LineRenderer ring, Vector3 center, float radius, Color color)
        {
            ring.startColor = color;
            ring.endColor = color;
            for (int i = 0; i < Segments; i++)
            {
                float angle = (i / (float)Segments) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + 0.05f,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }

        private void HideUnseen()
        {
            foreach (var pair in _rings)
            {
                if (pair.Value != null && !_seen.Contains(pair.Key))
                {
                    pair.Value.enabled = false;
                }
            }
        }

        private void HideAll()
        {
            foreach (var ring in _rings.Values)
            {
                if (ring != null)
                {
                    ring.enabled = false;
                }
            }
        }
    }
}
