using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;
using World.Biomes;

namespace World.Dressing.View
{
    /// <summary>
    /// The bind-time tone treatment (dressing-kit brief FR6): every kit material is rebuilt onto
    /// the project's URP Lit shader with its albedo pulled toward the biome's muted key — store
    /// packs are never used at native saturation, and their built-in-pipeline Standard materials
    /// (which would render broken under URP) never reach a renderer raw. One toned variant is
    /// created per (material, theme) and shared (assigned as sharedMaterial, so batching
    /// survives); the source asset is never modified. True desaturation of textured albedo is the
    /// P5-8 shader spike — until then the multiply-tint carries the invariant.
    /// </summary>
    public sealed class ToneMaterialCache
    {
        private const string PipelineShaderName = "Universal Render Pipeline/Lit";
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_Smoothness");

        /// <summary>Fallbacks for a biome with no authored appearance asset.</summary>
        private static readonly Color DefaultToneTint = new Color(0.55f, 0.57f, 0.52f);
        private const float DefaultToneStrength = 0.45f;

        /// <summary>Slight value pull-down so toned dressing sits below gameplay in the hierarchy.</summary>
        private const float ValueMultiplier = 0.9f;

        /// <summary>Flat low-poly look: no speculars competing with the gameplay layer.</summary>
        private const float TonedSmoothness = 0.1f;

        private readonly IBiomeAppearanceCatalog _appearanceCatalog;
        private readonly Dictionary<(Material, LevelTheme), Material> _variants =
            new Dictionary<(Material, LevelTheme), Material>();

        public ToneMaterialCache(IBiomeAppearanceCatalog appearanceCatalog)
        {
            _appearanceCatalog = appearanceCatalog;
        }

        public Material GetToned(Material source, LevelTheme theme)
        {
            if (source == null)
            {
                return null;
            }

            if (_variants.TryGetValue((source, theme), out var cached))
            {
                return cached;
            }

            var appearance = _appearanceCatalog?.Get(theme);
            Color tint = appearance != null ? appearance.ToneTint : DefaultToneTint;
            float strength = appearance != null ? appearance.ToneStrength : DefaultToneStrength;
            Color sourceColor = source.HasProperty("_Color") || source.HasProperty("_BaseColor")
                ? source.color
                : Color.white;
            Color toned = Color.Lerp(sourceColor, tint, strength) * ValueMultiplier;
            toned.a = sourceColor.a;

            var pipelineShader = Shader.Find(PipelineShaderName);
            Material variant;
            if (pipelineShader != null)
            {
                // Rebuild on the pipeline shader: the [MainTexture]/[MainColor] accessors carry
                // the albedo across from either a Standard (pack) or an URP source.
                variant = new Material(pipelineShader)
                {
                    mainTexture = source.mainTexture,
                    color = toned
                };
                if (variant.HasProperty(SmoothnessProperty))
                {
                    variant.SetFloat(SmoothnessProperty, TonedSmoothness);
                }
            }
            else
            {
                // No pipeline shader (headless/tests): clone and tint in place.
                variant = new Material(source) { color = toned };
            }

            variant.name = $"{source.name}_Toned_{theme}";
            _variants[(source, theme)] = variant;
            return variant;
        }
    }
}
