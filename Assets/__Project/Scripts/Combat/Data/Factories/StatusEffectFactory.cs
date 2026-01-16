using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;

namespace Combat.Data.Factories
{
    /// <summary>
    /// Factory that creates runtime status effect instances from ScriptableObject definitions.
    /// Pure C# class - all logic lives here, not in ScriptableObjects.
    /// </summary>
    public class StatusEffectFactory : IStatusEffectFactory
    {
        public IStatusEffect CreateStatusEffect(StatusEffectDefinition definition)
        {
            return CreateStatusEffect(definition, definition.Duration);
        }

        public IStatusEffect CreateStatusEffect(StatusEffectDefinition definition, int durationOverride)
        {
            return definition.Type switch
            {
                StatusEffectType.DamageOverTime => CreateDamageOverTimeEffect(definition, durationOverride),
                StatusEffectType.HealOverTime => CreateHealOverTimeEffect(definition, durationOverride),
                StatusEffectType.Control => CreateControlEffect(definition, durationOverride),
                StatusEffectType.Buff => CreateModifierEffect(definition, durationOverride, StatusEffectType.Buff),
                StatusEffectType.Debuff => CreateModifierEffect(definition, durationOverride, StatusEffectType.Debuff),
                _ => CreateBaseEffect(definition, durationOverride)
            };
        }

        private IStatusEffect CreateBaseEffect(StatusEffectDefinition def, int duration)
        {
            return new DataDrivenStatusEffect(
                def.Id,
                def.Name,
                def.Type,
                duration,
                1,
                def.IsStackable,
                def.MaxStacks,
                def.TriggerType,
                def.DamagePerTrigger,
                def.HealPerTrigger,
                def.DamagePerStack,
                def.HealPerStack,
                def.HpThreshold,
                def.ThresholdDirection);
        }

        private IStatusEffect CreateDamageOverTimeEffect(StatusEffectDefinition def, int duration)
        {
            return new DataDrivenDamageOverTimeEffect(
                def.Id,
                def.Name,
                duration,
                1,
                def.IsStackable,
                def.MaxStacks,
                def.TriggerType,
                def.DamagePerTrigger,
                def.DamagePerStack);
        }

        private IStatusEffect CreateHealOverTimeEffect(StatusEffectDefinition def, int duration)
        {
            return new DataDrivenHealOverTimeEffect(
                def.Id,
                def.Name,
                duration,
                1,
                def.IsStackable,
                def.MaxStacks,
                def.TriggerType,
                def.HealPerTrigger,
                def.HealPerStack);
        }

        private IStatusEffect CreateControlEffect(StatusEffectDefinition def, int duration)
        {
            return new DataDrivenControlEffect(
                def.Id,
                def.Name,
                duration,
                def.TriggerType);
        }

        private IStatusEffect CreateModifierEffect(StatusEffectDefinition def, int duration, StatusEffectType type)
        {
            // For debuffs, negate the modifier
            float modifier = type == StatusEffectType.Debuff
                ? -System.Math.Abs(def.StatModifier)
                : System.Math.Abs(def.StatModifier);

            return new DataDrivenModifierEffect(
                def.Id,
                def.Name,
                type,
                duration,
                1,
                def.IsStackable,
                modifier,
                def.TriggerType);
        }
    }
}
