using Platform;
using UnityEngine;

namespace LevelGeneration
{
    public class AreaView : MonoBehaviour
    {
        private IAreaGenerator areaGenerator;
        private IPlatform currentPlatform;
        
        public void Initialize(IAreaGenerator areaGenerator)
        {
            this.areaGenerator = areaGenerator;
            
            if (areaGenerator.EntryPlatform != null)
            {
                currentPlatform = areaGenerator.EntryPlatform;
            }
        }
        
        public void OnCharacterPlatformChanged(IPlatform newPlatform)
        {
            if (newPlatform != null && newPlatform != currentPlatform)
            {
                currentPlatform = newPlatform;
            }
        }
        
        void Start()
        {
            // If not initialized, try to find AreaGenerator
            if (areaGenerator == null)
            {
                // Could use dependency injection here
                Debug.LogWarning("AreaView: AreaGenerator not initialized. Call Initialize() first.");
            }
        }
    }
}

