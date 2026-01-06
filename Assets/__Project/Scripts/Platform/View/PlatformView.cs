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
        private int edgeVertexCount = 10;
        private float edgeJitter = 0.25f;
        private Color? platformColor;
        
        public void SetConfig(Material material, float thickness, int vertexCount, float jitter, Color? color = null)
        {
            platformMaterial = material;
            platformThickness = thickness;
            edgeVertexCount = vertexCount;
            edgeJitter = jitter;
            platformColor = color;
        }
        
        public void Initialize(IPlatform platform)
        {
            this.platform = platform;
            CreatePlatformGameObject();
        }
        
        private void CreatePlatformGameObject()
        {
            if (platform == null || platform.Visual == null) return;
            
            // Create mesh GameObject as child
            platformMeshObject = new GameObject("PlatformMesh");
            platformMeshObject.transform.SetParent(transform);
            platformMeshObject.transform.localPosition = Vector3.zero;
            
            // Build mesh
            var outline = new System.Collections.Generic.List<Vector3>();
            var mesh = PlatformMeshBuilder.BuildPlatformMesh(
                platform.Visual.Size.x,
                platform.Visual.Size.y,
                edgeVertexCount,
                edgeJitter,
                platformThickness,
                out outline
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
            
            // Build colliders (floor + walls) - parent to this GameObject
            PlatformColliderBuilder.BuildPlatformColliders(
                gameObject,
                outline,
                platformThickness
            );
            
            // Update visual boundary with the actual generated outline
            platform.Visual.TopBoundary = outline;
            
            // Note: Battlefield initialization for CombatPlatform is now handled by
            // CombatPlatformActiveState when the platform is entered.
            // The Combat system will use the TopBoundary set here.
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

