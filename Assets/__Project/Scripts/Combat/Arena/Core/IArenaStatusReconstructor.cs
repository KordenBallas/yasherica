using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Rebuilds a live status effect from its snapshot triple. The behaviour parameters (tick
    /// damage, control kind, modifier magnitude…) are NOT on the wire — they re-derive from the
    /// status's authored definition by id, which is the production implementation's job (the
    /// definition catalog + factory in the Data layer); tests supply a hand-built fake.
    /// </summary>
    public interface IArenaStatusReconstructor
    {
        bool TryRebuild(int effectId, int duration, int stackCount, out IStatusEffect effect);
    }
}
