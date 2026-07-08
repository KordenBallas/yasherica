using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Whether a committed intent's caster may still act when its step arrives. The resolver's
    /// default (alive and not stunned at that instant — the PvE skip-dead rule) applies when no
    /// policy is injected; Arena's step-batching mode (P4-3b) supplies a batch-scoped snapshot so
    /// a unit killed mid-batch still fires its same-batch step.
    /// </summary>
    public interface IIntentEligibility
    {
        bool CanAct(IUnit caster);
    }
}
