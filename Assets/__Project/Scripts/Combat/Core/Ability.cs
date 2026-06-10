namespace Combat.Core
{
    /// <summary>
    /// Base implementation of an ability.
    /// </summary>
    public class Ability : IAbility
    {
        public int Id { get; }
        public string Name { get; }
        public int CooldownDuration { get; }
        public AbilityShapeData Shape { get; }
        public AbilityEffectType EffectType { get; }

        public Ability(
            int id,
            string name,
            int cooldownDuration,
            AbilityShapeData shape,
            AbilityEffectType effectType)
        {
            Id = id;
            Name = name;
            CooldownDuration = cooldownDuration;
            Shape = shape;
            EffectType = effectType;
        }
    }
}
