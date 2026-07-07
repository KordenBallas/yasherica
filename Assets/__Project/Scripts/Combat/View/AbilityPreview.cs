using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Controller;
using UnityEngine;
using TMPro;

namespace Combat.View
{
    /// <summary>
    /// Shows preview of ability effects before execution.
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

        public void ShowAbilityPreview(IAbility ability, IUnit caster, IUnit target = null)
        {
            if (_previewPanel != null)
                _previewPanel.SetActive(true);

            if (_abilityNameText != null)
                _abilityNameText.text = ability.Name;

            if (_effectsText != null)
                _effectsText.text = BuildEffectsDescription(ability, target);

            if (_cooldownText != null)
            {
                _cooldownText.text = ability.CooldownDuration > 0
                    ? $"Cooldown: {ability.CooldownDuration} turns"
                    : "No cooldown";
            }
        }

        public void HidePreview()
        {
            if (_previewPanel != null)
                _previewPanel.SetActive(false);
        }

        private string BuildEffectsDescription(IAbility ability, IUnit target)
        {
            var shape = ability.Shape;
            string shapeText = shape.Type == AbilityShapeType.Ring
                ? $"Ring (radius {shape.RingRadius})"
                : $"Line (length {shape.LineLength})";

            string description = $"Shape: {shapeText}\n\n";

            if (ability is IDamageAbility damageAbility)
            {
                description += $"<color=red>Damage: {damageAbility.Damage}</color>\n";

                if (target != null)
                {
                    int resultingHP = System.Math.Max(0, target.CurrentHP - damageAbility.Damage);
                    description += $"Target HP: {target.CurrentHP} → {resultingHP}\n";

                    if (resultingHP == 0)
                        description += "<color=yellow>LETHAL</color>\n";
                }
            }

            if (ability is IHealAbility healAbility)
            {
                description += $"<color=green>Healing: {healAbility.HealAmount}</color>\n";

                if (target != null)
                {
                    int resultingHP = System.Math.Min(target.MaxHP, target.CurrentHP + healAbility.HealAmount);
                    description += $"Target HP: {target.CurrentHP} → {resultingHP}\n";
                }
            }

            if (ability is IStatusEffectAbility statusAbility)
            {
                var effect = statusAbility.EffectToApply;
                description += $"\n<color=orange>Status: {effect.Name}</color>\n";
                description += $"Duration: {statusAbility.EffectDuration} turns\n";

                if (effect is ITriggeredStatusEffect triggered)
                {
                    if (triggered.DamagePerTrigger > 0)
                        description += $"Damage per turn: {triggered.DamagePerTrigger}\n";
                    if (triggered.HealPerTrigger > 0)
                        description += $"Healing per turn: {triggered.HealPerTrigger}\n";
                }
            }

            return description;
        }
    }
}
