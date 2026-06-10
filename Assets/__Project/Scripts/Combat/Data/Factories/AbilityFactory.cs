using Combat.Core;
using Combat.Data.Definitions;

namespace Combat.Data.Factories
{
    /// <summary>
    /// Factory that creates runtime ability instances from ScriptableObject definitions.
    /// Pure C# class - all logic lives here, not in ScriptableObjects.
    /// </summary>
    public class AbilityFactory : IAbilityFactory
    {
        private readonly IStatusEffectFactory _statusEffectFactory;

        public AbilityFactory(IStatusEffectFactory statusEffectFactory)
        {
            _statusEffectFactory = statusEffectFactory;
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
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage);
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
                UnityEngine.Debug.LogWarning(
                    $"[AbilityFactory] StatusEffectAbility '{def.Name}' has no status effect assigned");
                return CreateBaseAbility(def);
            }

            var effect = _statusEffectFactory.CreateStatusEffect(def.StatusEffect);
            int duration = def.DurationOverride >= 0 ? def.DurationOverride : effect.Duration;

            return new DataDrivenStatusEffectAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), effect, duration);
        }

        private IAbility CreateHybridAbility(HybridAbilityDefinition def)
        {
            if (def.StatusEffect == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[AbilityFactory] HybridAbility '{def.Name}' has no status effect assigned, creating damage-only ability");
                return new DataDrivenDamageAbility(
                    def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage);
            }

            var effect = _statusEffectFactory.CreateStatusEffect(def.StatusEffect);
            int duration = def.DurationOverride >= 0 ? def.DurationOverride : effect.Duration;

            return new DataDrivenHybridAbility(
                def.Id, def.Name, def.CooldownDuration, BuildShape(def), def.Damage, effect, duration);
        }
    }
}
