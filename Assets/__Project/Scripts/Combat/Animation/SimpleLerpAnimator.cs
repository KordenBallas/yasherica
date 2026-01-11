using System.Collections;
using Combat.Config;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// Simple lerp-based movement animator.
    /// Uses animation curve and arc height for smooth movement.
    /// Can be easily replaced with more sophisticated animation later.
    /// </summary>
    public class SimpleLerpAnimator : ICharacterMovementAnimator
    {
        private readonly CombatMovementConfig _config;
        
        public SimpleLerpAnimator(CombatMovementConfig config)
        {
            _config = config;
        }
        
        public IEnumerator AnimateMovement(Transform character, Vector3 from, Vector3 to)
        {
            float elapsed = 0f;
            
            while (elapsed < _config.movementDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _config.movementDuration;
                float curveT = _config.movementCurve.Evaluate(t);
                
                Vector3 position = Vector3.Lerp(from, to, curveT);
                
                // Add arc height for jump effect
                position.y += Mathf.Sin(t * Mathf.PI) * _config.heightArcOffset;
                
                character.position = position;
                
                yield return null;
            }
            
            // Ensure final position is exact
            character.position = to;
        }
    }
}
