using Combat.Data.Definitions;

namespace Combat.Core.StatusEffects
{
    /// <summary>
    /// Base data-driven status effect with trigger support.
    /// Created by StatusEffectFactory from ScriptableObject definitions.
    /// </summary>
    public class DataDrivenStatusEffect : StatusEffect, ITriggeredStatusEffect
    {
        public StatusEffectTriggerType TriggerType { get; }
        public int DamagePerTrigger { get; }
        public int HealPerTrigger { get; }
        public int DamagePerStack { get; }
        public int HealPerStack { get; }
        public float HpThreshold { get; }
        public ThresholdDirection ThresholdDirection { get; }
        public int MaxStacks { get; }

        public DataDrivenStatusEffect(
            int id,
            string name,
            StatusEffectType type,
            int duration,
            int stackCount,
            bool isStackable,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int damagePerTrigger,
            int healPerTrigger,
            int damagePerStack,
            int healPerStack,
            float hpThreshold,
            ThresholdDirection thresholdDirection)
            : base(id, name, type, duration, stackCount, isStackable)
        {
            TriggerType = triggerType;
            DamagePerTrigger = damagePerTrigger;
            HealPerTrigger = healPerTrigger;
            DamagePerStack = damagePerStack;
            HealPerStack = healPerStack;
            MaxStacks = maxStacks;
            HpThreshold = hpThreshold;
            ThresholdDirection = thresholdDirection;
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenStatusEffect(
                Id, Name, Type, Duration - 1, StackCount,
                IsStackable, MaxStacks, TriggerType,
                DamagePerTrigger, HealPerTrigger,
                DamagePerStack, HealPerStack,
                HpThreshold, ThresholdDirection);
        }

        public override StatusEffect AddStack()
        {
            if (!IsStackable)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenStatusEffect(
                Id, Name, Type, Duration, StackCount + 1,
                IsStackable, MaxStacks, TriggerType,
                DamagePerTrigger + DamagePerStack,
                HealPerTrigger + HealPerStack,
                DamagePerStack, HealPerStack,
                HpThreshold, ThresholdDirection);
        }
    }

    /// <summary>
    /// Data-driven damage over time effect.
    /// Replaces hardcoded PoisonEffect when using ScriptableObject definitions.
    /// </summary>
    public class DataDrivenDamageOverTimeEffect : DataDrivenStatusEffect
    {
        public DataDrivenDamageOverTimeEffect(
            int id,
            string name,
            int duration,
            int stackCount,
            bool isStackable,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int damagePerTrigger,
            int damagePerStack)
            : base(id, name, StatusEffectType.DamageOverTime, duration, stackCount,
                   isStackable, maxStacks, triggerType,
                   damagePerTrigger, 0, damagePerStack, 0, 0f, ThresholdDirection.Below)
        {
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenDamageOverTimeEffect(
                Id, Name, Duration - 1, StackCount,
                IsStackable, MaxStacks, TriggerType,
                DamagePerTrigger, DamagePerStack);
        }

        public override StatusEffect AddStack()
        {
            if (!IsStackable)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenDamageOverTimeEffect(
                Id, Name, Duration, StackCount + 1,
                IsStackable, MaxStacks, TriggerType,
                DamagePerTrigger + DamagePerStack, DamagePerStack);
        }
    }

    /// <summary>
    /// Data-driven heal over time effect.
    /// Replaces hardcoded RegenerationEffect when using ScriptableObject definitions.
    /// </summary>
    public class DataDrivenHealOverTimeEffect : DataDrivenStatusEffect
    {
        public DataDrivenHealOverTimeEffect(
            int id,
            string name,
            int duration,
            int stackCount,
            bool isStackable,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int healPerTrigger,
            int healPerStack)
            : base(id, name, StatusEffectType.HealOverTime, duration, stackCount,
                   isStackable, maxStacks, triggerType,
                   0, healPerTrigger, 0, healPerStack, 0f, ThresholdDirection.Below)
        {
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenHealOverTimeEffect(
                Id, Name, Duration - 1, StackCount,
                IsStackable, MaxStacks, TriggerType,
                HealPerTrigger, HealPerStack);
        }

        public override StatusEffect AddStack()
        {
            if (!IsStackable)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenHealOverTimeEffect(
                Id, Name, Duration, StackCount + 1,
                IsStackable, MaxStacks, TriggerType,
                HealPerTrigger + HealPerStack, HealPerStack);
        }
    }

    /// <summary>
    /// Data-driven control effect (stun, root, etc.).
    /// Replaces hardcoded StunEffect when using ScriptableObject definitions.
    /// </summary>
    public class DataDrivenControlEffect : StatusEffect, ITriggeredStatusEffect
    {
        public StatusEffectTriggerType TriggerType { get; }
        public int DamagePerTrigger => 0;
        public int HealPerTrigger => 0;
        public float HpThreshold => 0f;
        public ThresholdDirection ThresholdDirection => ThresholdDirection.Below;

        public DataDrivenControlEffect(
            int id,
            string name,
            int duration,
            StatusEffectTriggerType triggerType)
            : base(id, name, StatusEffectType.Control, duration, 1, false)
        {
            TriggerType = triggerType;
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenControlEffect(Id, Name, Duration - 1, TriggerType);
        }
    }

    /// <summary>
    /// Data-driven stat modifier effect for buffs and debuffs.
    /// Modifies unit stats by a percentage while active.
    /// </summary>
    public class DataDrivenModifierEffect : StatusEffect, ITriggeredStatusEffect
    {
        public float StatModifier { get; }
        public StatusEffectTriggerType TriggerType { get; }
        public int DamagePerTrigger => 0;
        public int HealPerTrigger => 0;
        public float HpThreshold => 0f;
        public ThresholdDirection ThresholdDirection => ThresholdDirection.Below;

        public DataDrivenModifierEffect(
            int id,
            string name,
            StatusEffectType type,
            int duration,
            int stackCount,
            bool isStackable,
            float statModifier,
            StatusEffectTriggerType triggerType)
            : base(id, name, type, duration, stackCount, isStackable)
        {
            StatModifier = statModifier;
            TriggerType = triggerType;
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenModifierEffect(
                Id, Name, Type, Duration - 1, StackCount,
                IsStackable, StatModifier, TriggerType);
        }

        public override StatusEffect AddStack()
        {
            if (!IsStackable)
                return this;

            return new DataDrivenModifierEffect(
                Id, Name, Type, Duration, StackCount + 1,
                IsStackable, StatModifier, TriggerType);
        }
    }
}
