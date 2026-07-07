using Combat.Core;
using Combat.Data.Definitions;
using Core.Logging;

namespace Combat.Data.Factories
{
    /// <summary>
    /// Factory that creates runtime ability instances from ScriptableObject definitions.
    /// Pure C# class - all logic lives here, not in ScriptableObjects.
    /// </summary>
    public class AbilityFactory : IAbilityFactory
    {
        private readonly IStatusEffectFactory _statusEffectFactory;
        private readonly IGameLogger _logger;

        public AbilityFactory(IStatusEffectFactory statusEffectFactory, IGameLogger logger)
        {
            _statusEffectFactory = statusEffectFactory;
            _logger = logger;
        }

        public IAbility CreateAbility(AbilityDefinition definition)
        {
            return definition switch
            {
                HybridAbilityDefinition hybrid => CreateHybridAbility(hybrid),
                DamageAbilityDefinition damage => CreateDamageAbility(damage),
                HealAbilityDefinition heal => CreateHealAbility(heal),
                StatusEffectAbilityDefinition status => CreateStatusEffectAbility(status),
                _ => CreateBaseAbility(definition)
            };
        }

        public IAbilityInstance CreateAbilityInstance(AbilityDefinition definition)
        {
            var ability = CreateAbility(definition);
            return new AbilityInstance(ability, 0);
        }

        private static AbilityShapeData BuildShape(AbilityDefinition def)
        {
            return def.Shape == AbilityShapeType.Ring
                ? AbilityShapeData.ForRing(def.RingRadius)
                : AbilityShapeData.ForLine(def.LineLength);
        }

        private IAbility CreateBaseAbility(AbilityDefinition def)
        {
            return new Ability(def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.EffectType);
        }

        private IAbility CreateDamageAbility(DamageAbilityDefinition def)
        {
            return new DataDrivenDamageAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage, def.PushDistance);
        }

        private IAbility CreateHealAbility(HealAbilityDefinition def)
        {
            return new DataDrivenHealAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.HealAmount);
        }

        private IAbility CreateStatusEffectAbility(StatusEffectAbilityDefinition def)
        {
            if (def.StatusEffect == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[AbilityFactory] StatusEffectAbility '{def.Name}' has no status effect assigned");
                return CreateBaseAbility(def);
            }

            // Resolve the override BEFORE building the effect so the applied instance actually
            // carries the authored duration (an ability declares status + magnitude + duration).
            int duration = def.DurationOverride >= 0 ? def.DurationOverride : def.StatusEffect.Duration;
            var effect = _statusEffectFactory.CreateStatusEffect(def.StatusEffect, duration);

            return new DataDrivenStatusEffectAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), effect, duration);
        }

        private IAbility CreateHybridAbility(HybridAbilityDefinition def)
        {
            if (def.StatusEffect == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[AbilityFactory] HybridAbility '{def.Name}' has no status effect assigned, creating damage-only ability");
                return new DataDrivenDamageAbility(
                    def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage, def.PushDistance);
            }

            // Same override-first order as CreateStatusEffectAbility (see comment there).
            int duration = def.DurationOverride >= 0 ? def.DurationOverride : def.StatusEffect.Duration;
            var effect = _statusEffectFactory.CreateStatusEffect(def.StatusEffect, duration);

            return new DataDrivenHybridAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage, effect, duration,
                def.PushDistance);
        }
    }
}
