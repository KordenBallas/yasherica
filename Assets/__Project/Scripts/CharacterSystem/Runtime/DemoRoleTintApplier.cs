using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Tints an assembled rig via MaterialPropertyBlock so shared part materials are never mutated:
    /// the same placeholder mesh can read green on a villager and maroon on a bandit at once. Writes
    /// both URP's _BaseColor and the legacy _Color so it covers Lit and Unlit/Standard shaders.
    /// </summary>
    public sealed class DemoRoleTintApplier : IDemoRoleTintApplier
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Alpha 0 is the "no tint" sentinel — authored default on every SO field.</summary>
        public static bool ShouldTint(Color tint) => tint.a > 0f;

        public void Apply(GameObject root, Color tint)
        {
            if (root == null || !ShouldTint(tint))
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, tint);
                block.SetColor(ColorId, tint);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
