using UnityEngine;

namespace Platform
{
    public class PlatformView : MonoBehaviour
    {
        private IPlatform platform;
        private GameObject platformMeshObject;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        
        private Material platformMaterial;
        private float platformThickness = 1.0f;
        private float rimDropHeight = 0.4f;
        private float cellInset = 0.06f;
        private Color? platformColor;

        public void SetConfig(Material material, float thickness, float rimDrop, float inset, Color? color = null)
        {
            platformMaterial = material;
            platformThickness = thickness;
            rimDropHeight = rimDrop;
            cellInset = inset;
            platformColor = color;
        }

        public void Initialize(IPlatform platform)
        {
            this.platform = platform;
            CreatePlatformGameObject();
        }

        private void CreatePlatformGameObject()
        {
            if (platform == null || platform.Visual == null || platform.Visual.Surface == null) return;

            // Create mesh GameObject as child
            platformMeshObject = new GameObject("PlatformMesh");
            platformMeshObject.transform.SetParent(transform);
            platformMeshObject.transform.localPosition = Vector3.zero;

            // Build mesh from the hex surface (the same source of truth the combat grid reads).
            var mesh = PlatformHexSurfaceMeshBuilder.Build(
                platform.Visual.Surface,
                platformThickness,
                rimDropHeight,
                cellInset
            );
            
            // Add MeshFilter and MeshRenderer
            var meshFilter = platformMeshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            
            meshRenderer = platformMeshObject.AddComponent<MeshRenderer>();
            if (platformMaterial != null)
            {
                // Create material instance if color variation is enabled
                if (platformColor.HasValue)
                {
                    var mat = new Material(platformMaterial);
                    mat.color = platformColor.Value;
                    meshRenderer.sharedMaterial = mat;
                }
                else
                {
                    meshRenderer.sharedMaterial = platformMaterial;
                }
            }
            else
            {
                // Create default material
                var mat = new Material(Shader.Find("Standard"));
                mat.color = platformColor ?? Color.gray;
                meshRenderer.sharedMaterial = mat;
            }
            
            // Add MeshCollider
            meshCollider = platformMeshObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            // Walls sit on the stitched walkable outline (hex-union edge with its between-cell
            // notches sewn shut), which is what makes the decorative rim physically non-walkable:
            // its geometry lies beyond the wall colliders.
            PlatformColliderBuilder.BuildPlatformColliders(
                gameObject,
                platform.Visual.TopBoundary,
                platformThickness
            );

            // Note: Battlefield initialization is handled by CombatActiveState when the platform is
            // entered; TopBoundary was set from the surface outline at generation time and is final.
        }
        
        public void SetActive(bool active)
        {
            // Activate/deactivate the entire platform GameObject
            gameObject.SetActive(active);
        }
        
        void OnDestroy()
        {
            if (platformMeshObject != null)
            {
                Destroy(platformMeshObject);
            }
        }
    }
}

