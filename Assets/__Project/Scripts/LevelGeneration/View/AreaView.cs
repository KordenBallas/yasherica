using Platform;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace LevelGeneration
{
    public class AreaView : MonoBehaviour
    {
        private IAreaGenerator areaGenerator;
        private IPlatform currentPlatform;
        [Inject] private IGameLogger _logger;
        
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
                _logger?.Warning(LogCategory.LevelGeneration, "AreaView: AreaGenerator not initialized. Call Initialize() first.");
            }
        }
    }
}

