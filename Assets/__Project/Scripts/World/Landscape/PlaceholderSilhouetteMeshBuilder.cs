using UnityEngine;

namespace World.Landscape
{
    /// <summary>
    /// Procedural low-poly silhouette meshes for the landscape read while the real decoration
    /// assets (P5-4) are authored: routing-landmark peaks/domes, backdrop ridge strips, and the sky
    /// gradient quad. All color is baked as vertex color (rendered with an unlit vertex-color
    /// material) so the haze fade needs no fog system, per the muted render look.
    /// </summary>
    public static class PlaceholderSilhouetteMeshBuilder
    {
        private const int SilhouetteSides = 6;

        /// <summary>Unit-height six-sided peak (mountain/cave landmark), base on y = 0.</summary>
        public static Mesh BuildPeak(Color tint)
        {
            return BuildCone(baseRadius: 0.6f, topRadius: 0f, height: 1f, tint);
        }

        /// <summary>Squat six-sided dome (dune/tree-clump landmark), base on y = 0.</summary>
        public static Mesh BuildDome(Color tint)
        {
            return BuildCone(baseRadius: 0.7f, topRadius: 0.3f, height: 0.55f, tint);
        }

        /// <summary>
        /// Vertical ridge strip spanning local X, facing -Z: bottom edge at -baseDrop, top edge
        /// following <paramref name="heights"/>. Tops fade toward the haze tint so the far horizon
        /// reads as atmosphere, not a cut-out.
        /// </summary>
        public static Mesh BuildRidgeStrip(float[] heights, float width, float baseDrop, Color ridgeTint, Color hazeTint)
        {
            int count = heights.Length;
            var vertices = new Vector3[count * 2];
            var colors = new Color[count * 2];
            var triangles = new int[(count - 1) * 6];

            for (int i = 0; i < count; i++)
            {
                float x = i / (float)(count - 1) * width - width * 0.5f;
                vertices[i] = new Vector3(x, -baseDrop, 0f);
                vertices[count + i] = new Vector3(x, heights[i], 0f);
                colors[i] = ridgeTint;
                colors[count + i] = Color.Lerp(ridgeTint, hazeTint, 0.65f);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int t = i * 6;
                triangles[t] = i;
                triangles[t + 1] = count + i;
                triangles[t + 2] = count + i + 1;
                triangles[t + 3] = i;
                triangles[t + 4] = count + i + 1;
                triangles[t + 5] = i + 1;
            }

            return BuildMesh("RidgeStrip", vertices, colors, triangles);
        }

        /// <summary>Sky gradient quad facing -Z: horizon tint at the bottom, top tint at the top.</summary>
        public static Mesh BuildSkyQuad(float width, float height, Color topTint, Color horizonTint)
        {
            var vertices = new[]
            {
                new Vector3(-width * 0.5f, 0f, 0f),
                new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(-width * 0.5f, height, 0f),
                new Vector3(width * 0.5f, height, 0f)
            };
            var colors = new[] { horizonTint, horizonTint, topTint, topTint };
            var triangles = new[] { 0, 2, 3, 0, 3, 1 };

            return BuildMesh("SkyQuad", vertices, colors, triangles);
        }

        private static Mesh BuildCone(float baseRadius, float topRadius, float height, Color tint)
        {
            bool pointed = topRadius <= 0f;
            int baseCount = SilhouetteSides;
            int topCount = pointed ? 1 : SilhouetteSides;

            var vertices = new Vector3[baseCount + topCount];
            for (int i = 0; i < baseCount; i++)
            {
                float angle = i / (float)baseCount * 2f * Mathf.PI;
                vertices[i] = new Vector3(Mathf.Cos(angle) * baseRadius, 0f, Mathf.Sin(angle) * baseRadius);
                if (!pointed)
                {
                    vertices[baseCount + i] = new Vector3(
                        Mathf.Cos(angle) * topRadius, height, Mathf.Sin(angle) * topRadius);
                }
            }

            if (pointed)
            {
                vertices[baseCount] = new Vector3(0f, height, 0f);
            }

            var triangles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < baseCount; i++)
            {
                int next = (i + 1) % baseCount;
                if (pointed)
                {
                    triangles.Add(i);
                    triangles.Add(baseCount);
                    triangles.Add(next);
                }
                else
                {
                    triangles.Add(i);
                    triangles.Add(baseCount + i);
                    triangles.Add(baseCount + next);
                    triangles.Add(i);
                    triangles.Add(baseCount + next);
                    triangles.Add(next);
                }
            }

            if (!pointed)
            {
                // Flat cap: fan around the first top-ring vertex.
                for (int i = 1; i < baseCount - 1; i++)
                {
                    triangles.Add(baseCount);
                    triangles.Add(baseCount + i + 1);
                    triangles.Add(baseCount + i);
                }
            }

            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = tint;
            }

            return BuildMesh(pointed ? "PeakSilhouette" : "DomeSilhouette", vertices, colors, triangles.ToArray());
        }

        private static Mesh BuildMesh(string name, Vector3[] vertices, Color[] colors, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
