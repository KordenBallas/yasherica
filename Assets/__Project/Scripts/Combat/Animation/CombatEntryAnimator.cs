using System.Collections;
using Combat.Battlefield;
using Combat.Config;
using Core.Logging;
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
        private readonly IGameLogger _logger;

        public CombatEntryAnimator(
            ICharacterMovementAnimator movementAnimator,
            CombatMovementConfig config,
            IGameLogger logger)
        {
            _movementAnimator = movementAnimator;
            _config = config;
            _logger = logger;
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
                _logger.Info(LogCategory.Combat,$"[CombatEntryAnimator] Character already at valid cell: {approximate}");
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
            
            _logger.Info(LogCategory.Combat,$"[CombatEntryAnimator] Found closest cell: {closest} (distance: {minDistance})");
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
            Vector3 targetWorld = UnitGrounding.Grounded(
                battlefield.HexToWorld(targetCell),
                UnitGrounding.FeetOffsetFor(character));
            _logger.Info(LogCategory.Combat,$"[CombatEntryAnimator] Animating entry from {character.position} to {targetWorld}");
            
            yield return _movementAnimator.AnimateMovement(character, character.position, targetWorld);
            
            _logger.Info(LogCategory.Combat,$"[CombatEntryAnimator] Entry animation complete");
        }
    }
}
