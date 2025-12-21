using Battlefield;
using UnityEngine;
using Zenject;

namespace Platform
{
    public class CombatPlatform : Platform
    {
        public IBattlefield Battlefield { get; private set; }
        
        public CombatPlatform(int id) : base(id)
        {
        }
        
        public void InitializeBattlefield(IBattlefield battlefield, float hexSize = 2f)
        {
            Battlefield = battlefield;
            
            // Initialize battlefield based on platform visual boundary
            if (Visual != null && Visual.TopBoundary != null && Visual.TopBoundary.Count > 0)
            {
                Vector3 center = Visual.Position;
                HexOrientation orientation = HexOrientation.Flat;  // Default, can be configured
                
                Battlefield.Initialize(Visual.TopBoundary, center, hexSize, orientation);
            }
        }
        
        public class Factory : PlaceholderFactory<CombatPlatform>
        {
        }
    }
}
