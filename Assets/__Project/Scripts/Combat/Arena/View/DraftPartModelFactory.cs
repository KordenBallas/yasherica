using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>
    /// Builds a static display model for one draftable part: the part prefab's skinned mesh
    /// extracted at bind pose onto a plain MeshRenderer (standalone skinned meshes have no
    /// bones to deform), bounds-normalised to a pedestal size, with a fitted BoxCollider for
    /// stage raycasts. Falls back to the part's choice icon on a sprite quad when the prefab
    /// carries no usable mesh. Placeholder-art fidelity by design (real thumbnails = ROADMAP).
    /// </summary>
    public static class DraftPartModelFactory
    {
        private const float FallbackSpriteScale = 0.8f;

        public static GameObject CreateDisplayModel(PartDefinition part, Transform parent, float targetSize)
        {
            var root = new GameObject($"PartModel_{part.Id}");
            root.transform.SetParent(parent, worldPositionStays: false);

            var skinned = part.PartPrefab != null
                ? part.PartPrefab.GetComponentInChildren<SkinnedMeshRenderer>()
                : null;
            if (skinned != null && skinned.sharedMesh != null)
            {
                BuildMeshDisplay(root, skinned, targetSize);
            }
            else
            {
                BuildSpriteFallback(root, part, targetSize);
            }

            return root;
        }

        private static void BuildMeshDisplay(GameObject root, SkinnedMeshRenderer skinned, float targetSize)
        {
            var display = new GameObject("Mesh");
            display.transform.SetParent(root.transform, worldPositionStays: false);

            var filter = display.AddComponent<MeshFilter>();
            filter.sharedMesh = skinned.sharedMesh;
            var renderer = display.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = skinned.sharedMaterials;

            // Bind-pose meshes sit at arbitrary offsets/scales — normalise by bounds so every
            // pedestal reads the same size with the mesh centred above the pedestal origin.
            var bounds = skinned.sharedMesh.bounds;
            float largestExtent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = largestExtent > Mathf.Epsilon ? targetSize / largestExtent : 1f;
            display.transform.localScale = Vector3.one * scale;
            display.transform.localPosition =
                -bounds.center * scale + Vector3.up * (bounds.extents.y * scale);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.up * (bounds.extents.y * scale);
            collider.size = bounds.size * scale;
        }

        private static void BuildSpriteFallback(GameObject root, PartDefinition part, float targetSize)
        {
            var display = new GameObject("Sprite");
            display.transform.SetParent(root.transform, worldPositionStays: false);
            display.transform.localPosition = Vector3.up * (targetSize * 0.5f);

            var sprite = display.AddComponent<SpriteRenderer>();
            sprite.sprite = part.ChoiceIcon;
            display.transform.localScale = Vector3.one * (targetSize * FallbackSpriteScale);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.up * (targetSize * 0.5f);
            collider.size = new Vector3(targetSize, targetSize, 0.1f);
        }
    }
}
