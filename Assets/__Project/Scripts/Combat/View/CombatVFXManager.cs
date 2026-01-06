using Combat.Core;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Visual effects manager for combat animations and VFX.
    /// Minimal implementation - can be extended with particle systems and animations.
    /// </summary>
    public class CombatVFXManager : MonoBehaviour
    {
        [Header("VFX Prefabs")]
        [SerializeField] private GameObject _damageEffectPrefab;
        [SerializeField] private GameObject _healEffectPrefab;
        [SerializeField] private GameObject _poisonEffectPrefab;
        [SerializeField] private GameObject _stunEffectPrefab;
        
        /// <summary>
        /// Plays damage effect at a position.
        /// </summary>
        public void PlayDamageEffect(Vector3 position, int damage)
        {
            if (_damageEffectPrefab != null)
            {
                var effect = Instantiate(_damageEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Show floating damage text
            ShowFloatingText(position, $"-{damage}", Color.red);
        }
        
        /// <summary>
        /// Plays healing effect at a position.
        /// </summary>
        public void PlayHealEffect(Vector3 position, int amount)
        {
            if (_healEffectPrefab != null)
            {
                var effect = Instantiate(_healEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Show floating heal text
            ShowFloatingText(position, $"+{amount}", Color.green);
        }
        
        /// <summary>
        /// Plays status effect visual.
        /// </summary>
        public void PlayStatusEffectVFX(Vector3 position, IStatusEffect effect)
        {
            GameObject prefab = effect.Type switch
            {
                StatusEffectType.DamageOverTime => _poisonEffectPrefab,
                StatusEffectType.Control => _stunEffectPrefab,
                _ => null
            };
            
            if (prefab != null)
            {
                var vfx = Instantiate(prefab, position, Quaternion.identity);
                Destroy(vfx, 1.5f);
            }
        }
        
        /// <summary>
        /// Shows floating damage/heal text (simplified).
        /// In a real implementation, this would use TextMeshPro and animation.
        /// </summary>
        private void ShowFloatingText(Vector3 position, string text, Color color)
        {
            // This is a placeholder for actual floating text implementation
            Debug.Log($"Floating text at {position}: {text}");
        }
        
        /// <summary>
        /// Plays movement trail effect.
        /// </summary>
        public void PlayMovementTrail(Vector3 from, Vector3 to)
        {
            // Placeholder for movement trail effect
            Debug.Log($"Movement trail from {from} to {to}");
        }
    }
}

