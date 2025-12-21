using UnityEngine;
using Battlefield;

namespace Platform
{
    public class PlatformView : MonoBehaviour
    {
        private IPlatform platform;
        private GameObject platformMeshObject;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private BattlefieldView battlefieldView;
        //private Battlefield.BattlefieldView battlefieldView;
        
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
            
            // Re-initialize battlefield for combat platforms (after actual boundary is set)
            // This is critical because the battlefield was initialized with a simple rectangular boundary,
            // but now we have the actual jittered outline from the mesh builder
            if (platform is CombatPlatform combatPlatform)
            {
                // Re-initialize battlefield with the actual boundary
                if (combatPlatform.Battlefield != null)
                {
                    Vector3 center = platform.Visual.Position;
                    float hexSize = combatPlatform.Battlefield.HexSize;
                    HexOrientation orientation = HexOrientation.Flat; // Default
                    combatPlatform.Battlefield.Initialize(outline, center, hexSize, orientation);
                }
                else
                {
                    InitializeCombatPlatformBattlefield(combatPlatform);
                }
                
                // Now create/update the visual representation
                if (battlefieldView != null)
                {
                    // Regenerate if view already exists
                    battlefieldView.Regenerate();
                }
                else
                {
                    InitializeBattlefieldView(combatPlatform);
                }
            }
        }
        
        private void InitializeCombatPlatformBattlefield(CombatPlatform combatPlatform)
        {
            // Battlefield should be initialized by the platform factory or AreaGenerator
            // If it's not initialized yet, we'll create it here with default hex size
            if (combatPlatform.Battlefield == null)
            {
                // Create battlefield using factory (if available) or directly
                var battlefield = new Battlefield.Battlefield();
                combatPlatform.InitializeBattlefield(battlefield, hexSize: 2f); // Default hex size
            }
            
            // Now create the visual representation
            if (combatPlatform.Battlefield != null)
            {
                InitializeBattlefieldView(combatPlatform);
            }
        }
        
        private void InitializeBattlefieldView(CombatPlatform combatPlatform)
        {
            // Create battlefield view GameObject
            var battlefieldGO = new GameObject("BattlefieldView");
            battlefieldGO.transform.SetParent(transform);
            battlefieldGO.transform.localPosition = Vector3.zero;
            
            battlefieldView = battlefieldGO.AddComponent<BattlefieldView>();
            battlefieldView.Initialize(combatPlatform.Battlefield);
            
            // Battlefield is enabled by default
            battlefieldView.SetActive(true);
        }
        
        public void SetActive(bool active)
        {
            // Activate/deactivate the entire platform GameObject
            gameObject.SetActive(active);
            
            // Update battlefield visibility based on platform state
            if (battlefieldView != null && platform is CombatPlatform combatPlatform)
            {
                // Show battlefield when platform is active
                bool isActiveState = combatPlatform.StateMachine.CurrentState is PlatformActiveState;
                battlefieldView.SetActive(active && isActiveState);
            }
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

