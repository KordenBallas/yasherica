using System.Collections;
using Combat.Battlefield;
using Combat.Config;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// Handles animation for character entering combat.
    /// Finds closest cell and animates character to it.
    /// </summary>
    public class CombatEntryAnimator
    {
        private readonly ICharacterMovementAnimator _movementAnimator;
        private readonly CombatMovementConfig _config;
        
        public CombatEntryAnimator(
            ICharacterMovementAnimator movementAnimator,
            CombatMovementConfig config)
        {
            _movementAnimator = movementAnimator;
            _config = config;
        }
        
        /// <summary>
        /// Finds the closest valid cell on the battlefield to the given world position.
        /// </summary>
        public HexCoordinates FindClosestCell(Vector3 worldPosition, IBattlefield battlefield)
        {
            // Convert world position to hex
            HexCoordinates approximate = battlefield.WorldToHex(worldPosition);
            
            // Verify it's in boundary
            if (battlefield.IsCellInBoundary(approximate))
            {
                Debug.Log($"[CombatEntryAnimator] Character already at valid cell: {approximate}");
                return approximate;
            }
            
            // Find closest valid cell
            var allCells = battlefield.GetCellsInBoundary();
            HexCoordinates closest = allCells[0];
            float minDistance = float.MaxValue;
            
            foreach (var cell in allCells)
            {
                Vector3 cellWorld = battlefield.HexToWorld(cell);
                float distance = Vector3.Distance(worldPosition, cellWorld);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = cell;
                }
            }
            
            Debug.Log($"[CombatEntryAnimator] Found closest cell: {closest} (distance: {minDistance})");
            return closest;
        }
        
        /// <summary>
        /// Animates character entry to the specified cell.
        /// </summary>
        public IEnumerator AnimateEntryToCell(
            Transform character, 
            HexCoordinates targetCell, 
            IBattlefield battlefield)
        {
            Vector3 targetWorld = battlefield.HexToWorld(targetCell);
            Debug.Log($"[CombatEntryAnimator] Animating entry from {character.position} to {targetWorld}");
            
            yield return _movementAnimator.AnimateMovement(character, character.position, targetWorld);
            
            Debug.Log($"[CombatEntryAnimator] Entry animation complete");
        }
    }
}
