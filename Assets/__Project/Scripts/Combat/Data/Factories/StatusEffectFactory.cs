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
            // A freshly applied effect always starts at one stack; stacking happens on re-apply.
            return CreateStatusEffect(definition, durationOverride, stackCount: 1);
        }

        public IStatusEffect CreateStatusEffect(
            StatusEffectDefinition definition, int durationOverride, int stackCount)
        {
            return definition.Type switch
            {
                StatusEffectType.DamageOverTime => CreateDamageOverTimeEffect(definition, durationOverride, stackCount),
                StatusEffectType.HealOverTime => CreateHealOverTimeEffect(definition, durationOverride, stackCount),
                StatusEffectType.Control => CreateControlEffect(definition, durationOverride, stackCount),
                StatusEffectType.Buff => CreateModifierEffect(definition, durationOverride, StatusEffectType.Buff, stackCount),
                StatusEffectType.Debuff => CreateModifierEffect(definition, durationOverride, StatusEffectType.Debuff, stackCount),
                _ => CreateBaseEffect(definition, durationOverride, stackCount)
            };
        }

        private IStatusEffect CreateBaseEffect(StatusEffectDefinition def, int duration, int stackCount)
        {
            return new DataDrivenStatusEffect(
                def.Id,
                def.Name,
                def.Type,
                duration,
                stackCount,
                def.StackRule,
                def.MaxStacks,
                def.TriggerType,
                def.DamagePerTrigger,
                def.HealPerTrigger,
                def.DamagePerStack,
                def.HealPerStack,
                def.HpThreshold,
                def.ThresholdDirection);
        }

        private IStatusEffect CreateDamageOverTimeEffect(StatusEffectDefinition def, int duration, int stackCount)
        {
            return new DataDrivenDamageOverTimeEffect(
                def.Id,
                def.Name,
                duration,
                stackCount,
                def.StackRule,
                def.MaxStacks,
                def.TriggerType,
                def.DamagePerTrigger,
                def.DamagePerStack);
        }

        private IStatusEffect CreateHealOverTimeEffect(StatusEffectDefinition def, int duration, int stackCount)
        {
            return new DataDrivenHealOverTimeEffect(
                def.Id,
                def.Name,
                duration,
                stackCount,
                def.StackRule,
                def.MaxStacks,
                def.TriggerType,
                def.HealPerTrigger,
                def.HealPerStack);
        }

        private IStatusEffect CreateControlEffect(StatusEffectDefinition def, int duration, int stackCount)
        {
            return new DataDrivenControlEffect(
                def.Id,
                def.Name,
                duration,
                def.ControlKind,
                def.MovementPenalty,
                def.TriggerType,
                stackCount: stackCount,
                stackRule: def.StackRule,
                maxStacks: def.MaxStacks);
        }

        private IStatusEffect CreateModifierEffect(
            StatusEffectDefinition def, int duration, StatusEffectType type, int stackCount)
        {
            // The magnitude is authored SIGNED (Weakened = -5 outgoing, Hardened = -5 incoming):
            // Buff/Debuff stays an informational classification, never a sign flip.
            return new DataDrivenModifierEffect(
                def.Id,
                def.Name,
                type,
                duration,
                stackCount,
                def.StackRule,
                def.Magnitude,
                def.StatTarget,
                def.TriggerType,
                def.MaxStacks);
        }
    }
}
