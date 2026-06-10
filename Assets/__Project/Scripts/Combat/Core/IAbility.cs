namespace Combat.Core
{
    /// <summary>
    /// Represents a base ability that a unit can use.
    /// </summary>
    public interface IAbility
    {
        int Id { get; }
        string Name { get; }
        int CooldownDuration { get; }
        AbilityShapeData Shape { get; }
        AbilityEffectType EffectType { get; }
    }
}
