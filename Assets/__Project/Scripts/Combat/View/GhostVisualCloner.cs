using System.Collections.Generic;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Clones a unit's visual into an inert, translucent ghost: all behaviours, colliders
    /// and physics are stripped before the clone ever activates (so no combat component
    /// wakes up on it), and every renderer is swapped to a shared alpha-blend ghost material.
    /// </summary>
    public static class GhostVisualCloner
    {
        private static readonly Color GhostTint = new Color(0.55f, 0.8f, 1f, TelegraphStyle.GhostAlpha);
        private static Material _ghostMaterial;

        /// <summary>
        /// Creates the ghost clone as a child of <paramref name="parent"/>, inactive-safe.
        /// Returns the clone root and the renderers to fade.
        /// </summary>
        public static GameObject Clone(Transform source, Transform parent, out List<Renderer> renderers)
        {
            // An inactive holder defers Awake on cloned components until after stripping.
            var holder = new GameObject("GhostHolder");
            holder.transform.SetParent(parent, false);
            holder.SetActive(false);

            var clone = Object.Instantiate(source.gameObject, holder.transform);
            clone.name = "Ghost_" + source.name;
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            // Remove overhead clutter (plan-icons row, HP/name labels) so the ghost reads
            // as a clean unit silhouette, not a copy of the UI.
            var iconsRow = clone.transform.Find(TelegraphStyle.PlanIconsRowName);
            if (iconsRow != null)
                Object.DestroyImmediate(iconsRow.gameObject);
            foreach (var label in clone.GetComponentsInChildren<TMPro.TMP_Text>(true))
                Object.DestroyImmediate(label.gameObject);

            Strip<MonoBehaviour>(clone);
            Strip<Collider>(clone);
            Strip<Rigidbody>(clone);
            Strip<Animator>(clone);
            Strip<AudioSource>(clone);

            renderers = new List<Renderer>();
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var ghostMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < ghostMaterials.Length; i++)
                    ghostMaterials[i] = GetGhostMaterial();
                renderer.sharedMaterials = ghostMaterials;
                renderers.Add(renderer);
            }

            holder.SetActive(true);
            return holder;
        }

        private static void Strip<T>(GameObject root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
                Object.DestroyImmediate(component);
        }

        private static Material GetGhostMaterial()
        {
            if (_ghostMaterial != null)
                return _ghostMaterial;

            _ghostMaterial = new Material(Shader.Find("Standard")) { color = GhostTint };
            _ghostMaterial.SetFloat("_Mode", 3); // Transparent mode
            _ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _ghostMaterial.SetInt("_ZWrite", 0);
            _ghostMaterial.DisableKeyword("_ALPHATEST_ON");
            _ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
            _ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _ghostMaterial.renderQueue = 3000;
            return _ghostMaterial;
        }
    }
}
