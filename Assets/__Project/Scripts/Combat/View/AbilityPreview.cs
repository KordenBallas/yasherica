using Combat.Core;
using Combat.Controller;
using UnityEngine;
using TMPro;

namespace Combat.View
{
    /// <summary>
    /// Shows preview of ability effects before execution.
    /// Displays damage prediction, healing amounts, and status effects.
    /// </summary>
    public class AbilityPreview : MonoBehaviour
    {
        [SerializeField] private GameObject _previewPanel;
        [SerializeField] private TextMeshProUGUI _abilityNameText;
        [SerializeField] private TextMeshProUGUI _effectsText;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        private ICombatController _gameController;
        
        public void Initialize(ICombatController gameController)
        {
            _gameController = gameController;
            HidePreview();
        }
        
        /// <summary>
        /// Shows preview for a specific ability and target.
        /// </summary>
        public void ShowAbilityPreview(IAbility ability, IUnit caster, IUnit target = null)
        {
            if (_previewPanel != null)
            {
                _previewPanel.SetActive(true);
            }
            
            // Ability name
            if (_abilityNameText != null)
            {
                _abilityNameText.text = ability.Name;
            }
            
            // Effects description
            if (_effectsText != null)
            {
                string effects = BuildEffectsDescription(ability, caster, target);
                _effectsText.text = effects;
            }
            
            // Cooldown info
            if (_cooldownText != null)
            {
                if (ability.CooldownDuration > 0)
                {
                    _cooldownText.text = $"Cooldown: {ability.CooldownDuration} turns";
                }
                else
                {
                    _cooldownText.text = "No cooldown";
                }
            }
        }
        
        /// <summary>
        /// Hides the preview panel.
        /// </summary>
        public void HidePreview()
        {
            if (_previewPanel != null)
            {
                _previewPanel.SetActive(false);
            }
        }
        
        private string BuildEffectsDescription(IAbility ability, IUnit caster, IUnit target)
        {
            string description = $"Range: {ability.Range}\n";
            description += $"Target: {ability.TargetType}\n\n";
            
            // Damage
            if (ability is IDamageAbility damageAbility)
            {
                description += $"<color=red>Damage: {damageAbility.Damage}</color>\n";
                
                if (target != null)
                {
                    int resultingHP = System.Math.Max(0, target.CurrentHP - damageAbility.Damage);
                    description += $"Target HP: {target.CurrentHP} → {resultingHP}\n";
                    
                    if (resultingHP == 0)
                    {
                        description += "<color=yellow>LETHAL</color>\n";
                    }
                }
            }
            
            // Healing
            if (ability is IHealAbility healAbility)
            {
                description += $"<color=green>Healing: {healAbility.HealAmount}</color>\n";
                
                if (target != null)
                {
                    int resultingHP = System.Math.Min(target.MaxHP, target.CurrentHP + healAbility.HealAmount);
                    description += $"Target HP: {target.CurrentHP} → {resultingHP}\n";
                }
            }
            
            // Status effects
            if (ability is IStatusEffectAbility statusAbility)
            {
                var effect = statusAbility.EffectToApply;
                description += $"\n<color=orange>Status: {effect.Name}</color>\n";
                description += $"Duration: {statusAbility.EffectDuration} turns\n";
                
                if (effect is PoisonEffect poison)
                {
                    description += $"Damage per turn: {poison.DamagePerTurn}\n";
                }
                else if (effect is RegenerationEffect regen)
                {
                    description += $"Healing per turn: {regen.HealPerTurn}\n";
                }
            }
            
            return description;
        }
    }
}

