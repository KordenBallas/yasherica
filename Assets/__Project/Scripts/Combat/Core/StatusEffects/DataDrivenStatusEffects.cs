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
            StackRule stackRule,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int damagePerTrigger,
            int healPerTrigger,
            int damagePerStack,
            int healPerStack,
            float hpThreshold,
            ThresholdDirection thresholdDirection)
            : base(id, name, type, duration, stackCount, stackRule)
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
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger, HealPerTrigger,
                DamagePerStack, HealPerStack,
                HpThreshold, ThresholdDirection);
        }

        public override StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenStatusEffect(
                Id, Name, Type, Duration, StackCount + 1,
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger + DamagePerStack,
                HealPerTrigger + HealPerStack,
                DamagePerStack, HealPerStack,
                HpThreshold, ThresholdDirection);
        }

        public override StatusEffect WithDuration(int duration)
        {
            return new DataDrivenStatusEffect(
                Id, Name, Type, duration, StackCount,
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger, HealPerTrigger,
                DamagePerStack, HealPerStack,
                HpThreshold, ThresholdDirection);
        }
    }

    /// <summary>
    /// Data-driven damage over time effect (burn, poison, bleed, ...).
    /// </summary>
    public class DataDrivenDamageOverTimeEffect : DataDrivenStatusEffect
    {
        public DataDrivenDamageOverTimeEffect(
            int id,
            string name,
            int duration,
            int stackCount,
            StackRule stackRule,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int damagePerTrigger,
            int damagePerStack)
            : base(id, name, StatusEffectType.DamageOverTime, duration, stackCount,
                   stackRule, maxStacks, triggerType,
                   damagePerTrigger, 0, damagePerStack, 0, 0f, ThresholdDirection.Below)
        {
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenDamageOverTimeEffect(
                Id, Name, Duration - 1, StackCount,
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger, DamagePerStack);
        }

        public override StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenDamageOverTimeEffect(
                Id, Name, Duration, StackCount + 1,
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger + DamagePerStack, DamagePerStack);
        }

        public override StatusEffect WithDuration(int duration)
        {
            return new DataDrivenDamageOverTimeEffect(
                Id, Name, duration, StackCount,
                StackRule, MaxStacks, TriggerType,
                DamagePerTrigger, DamagePerStack);
        }
    }

    /// <summary>
    /// Data-driven heal over time effect (regen, ...).
    /// </summary>
    public class DataDrivenHealOverTimeEffect : DataDrivenStatusEffect
    {
        public DataDrivenHealOverTimeEffect(
            int id,
            string name,
            int duration,
            int stackCount,
            StackRule stackRule,
            int maxStacks,
            StatusEffectTriggerType triggerType,
            int healPerTrigger,
            int healPerStack)
            : base(id, name, StatusEffectType.HealOverTime, duration, stackCount,
                   stackRule, maxStacks, triggerType,
                   0, healPerTrigger, 0, healPerStack, 0f, ThresholdDirection.Below)
        {
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenHealOverTimeEffect(
                Id, Name, Duration - 1, StackCount,
                StackRule, MaxStacks, TriggerType,
                HealPerTrigger, HealPerStack);
        }

        public override StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenHealOverTimeEffect(
                Id, Name, Duration, StackCount + 1,
                StackRule, MaxStacks, TriggerType,
                HealPerTrigger + HealPerStack, HealPerStack);
        }

        public override StatusEffect WithDuration(int duration)
        {
            return new DataDrivenHealOverTimeEffect(
                Id, Name, duration, StackCount,
                StackRule, MaxStacks, TriggerType,
                HealPerTrigger, HealPerStack);
        }
    }

    /// <summary>
    /// Data-driven control effect. The kind decides what it restricts:
    /// Stun gates the whole turn (Unit.ActionState), Root/Slow gate movement
    /// (MovementRange.EffectiveFor).
    /// </summary>
    public class DataDrivenControlEffect : StatusEffect, ITriggeredStatusEffect
    {
        public ControlKind Kind { get; }
        public int MovementPenalty { get; }
        public int MaxStacks { get; }
        public StatusEffectTriggerType TriggerType { get; }
        public int DamagePerTrigger => 0;
        public int HealPerTrigger => 0;
        public float HpThreshold => 0f;
        public ThresholdDirection ThresholdDirection => ThresholdDirection.Below;

        public DataDrivenControlEffect(
            int id,
            string name,
            int duration,
            ControlKind kind,
            int movementPenalty,
            StatusEffectTriggerType triggerType,
            int stackCount = 1,
            StackRule stackRule = StackRule.Refresh,
            int maxStacks = 0)
            : base(id, name, StatusEffectType.Control, duration, stackCount, stackRule)
        {
            Kind = kind;
            MovementPenalty = movementPenalty;
            MaxStacks = maxStacks;
            TriggerType = triggerType;
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenControlEffect(
                Id, Name, Duration - 1, Kind, MovementPenalty, TriggerType, StackCount, StackRule, MaxStacks);
        }

        public override StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenControlEffect(
                Id, Name, Duration, Kind, MovementPenalty, TriggerType, StackCount + 1, StackRule, MaxStacks);
        }

        public override StatusEffect WithDuration(int duration)
        {
            return new DataDrivenControlEffect(
                Id, Name, duration, Kind, MovementPenalty, TriggerType, StackCount, StackRule, MaxStacks);
        }
    }

    /// <summary>
    /// Data-driven stat modifier effect for buffs and debuffs: one flat signed magnitude
    /// against one stat target. The same record backs a timed status and a part's
    /// duration-less passive (the shared modifier model, combat-status-effects FR10).
    /// </summary>
    public class DataDrivenModifierEffect : StatusEffect, ITriggeredStatusEffect
    {
        public int Magnitude { get; }
        public StatTarget Target { get; }
        public int MaxStacks { get; }
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
            StackRule stackRule,
            int magnitude,
            StatTarget target,
            StatusEffectTriggerType triggerType,
            int maxStacks = 0)
            : base(id, name, type, duration, stackCount, stackRule)
        {
            Magnitude = magnitude;
            Target = target;
            MaxStacks = maxStacks;
            TriggerType = triggerType;
        }

        public override StatusEffect DecrementDuration()
        {
            return new DataDrivenModifierEffect(
                Id, Name, Type, Duration - 1, StackCount,
                StackRule, Magnitude, Target, TriggerType, MaxStacks);
        }

        public override StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            if (MaxStacks > 0 && StackCount >= MaxStacks)
                return this;

            return new DataDrivenModifierEffect(
                Id, Name, Type, Duration, StackCount + 1,
                StackRule, Magnitude, Target, TriggerType, MaxStacks);
        }

        public override StatusEffect WithDuration(int duration)
        {
            return new DataDrivenModifierEffect(
                Id, Name, Type, duration, StackCount,
                StackRule, Magnitude, Target, TriggerType, MaxStacks);
        }
    }
}
