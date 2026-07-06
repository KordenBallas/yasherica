using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// One self-animating cell of the placeholder ability sweep (D3): a ground-aligned quad at a
    /// single affected cell that, after a start delay, pops in (scale 0→full) and brightens, holds,
    /// then fades out and destroys itself. Update-driven, no coroutine; owns its own material so each
    /// cell fades independently. Staggering the delay across cells (see <see cref="AbilityAreaSweep"/>)
    /// produces the line sweep / ring pop. Placeholder motion — replaceable by real VFX later.
    /// </summary>
    public sealed class AbilityCellFlash : MonoBehaviour
    {
        // The visible life splits into pop → hold → fade by these fractions of the active window.
        private const float PopFraction = 0.25f;
        private const float HoldFraction = 0.25f;
        private const float FadeFraction = 0.50f;
        private const float GroundYOffset = 0.06f; // lift slightly so the quad reads above the tile

        private Renderer _renderer;
        private Material _material;
        private Color _tint;
        private float _peakAlpha;
        private float _cellSize;

        private float _delay;
        private float _activeSeconds;
        private float _elapsed;

        public void Initialize(Vector3 worldPosition, float cellSize, float delay, Color tint,
            float peakAlpha, float activeSeconds)
        {
            _tint = tint;
            _peakAlpha = peakAlpha;
            _cellSize = cellSize;
            _delay = Mathf.Max(0f, delay);
            _activeSeconds = Mathf.Max(0.01f, activeSeconds);

            transform.position = worldPosition + Vector3.up * GroundYOffset;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lay the quad flat, normal up
            transform.localScale = Vector3.zero;

            BuildQuad();
            ApplyAlpha(0f);
        }

        private void BuildQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FlashQuad";
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;

            _renderer = quad.GetComponent<Renderer>();
            // Unlit transparent material driven by its own colour (Sprites/Default respects material.color).
            _material = new Material(Shader.Find("Sprites/Default"));
            _renderer.sharedMaterial = _material;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed < _delay)
                return;

            float t = _elapsed - _delay;
            float popSeconds = _activeSeconds * PopFraction;
            float holdSeconds = _activeSeconds * HoldFraction;
            float fadeSeconds = _activeSeconds * FadeFraction;

            if (t <= popSeconds)
            {
                float k = Mathf.Clamp01(t / popSeconds);
                transform.localScale = Vector3.one * (_cellSize * k);
                ApplyAlpha(_peakAlpha * k);
            }
            else if (t <= popSeconds + holdSeconds)
            {
                transform.localScale = Vector3.one * _cellSize;
                ApplyAlpha(_peakAlpha);
            }
            else if (t <= popSeconds + holdSeconds + fadeSeconds)
            {
                float k = Mathf.Clamp01((t - popSeconds - holdSeconds) / fadeSeconds);
                transform.localScale = Vector3.one * _cellSize;
                ApplyAlpha(Mathf.Lerp(_peakAlpha, 0f, k));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (_material == null)
                return;

            var color = _tint;
            color.a = alpha;
            _material.color = color;
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
