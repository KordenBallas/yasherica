namespace Narrative.Actors.Core
{
    /// <summary>
    /// Per-run mutable instance of an <see cref="NpcArchetypeData"/> (R2). Identity that persists
    /// across the run: a stable instance id, the archetype it came from, the chosen display name, and
    /// its faction. Per-actor facts live in the shared store under <c>actor.&lt;InstanceId&gt;.*</c>, so
    /// reusing the same instance across castings gives recurring-actor carry-over (R12).
    ///
    /// Identity policy (D2): re-casting an archetype yields a NEW instance by default; reuse is an
    /// explicit decision made by the caller (it keeps the same instance).
    /// </summary>
    public sealed class NpcInstance
    {
        public string InstanceId { get; }
        public string ArchetypeId { get; }
        public string ChosenDisplayName { get; }
        public string FactionId { get; }

        public NpcInstance(string instanceId, string archetypeId, string chosenDisplayName, string factionId)
        {
            InstanceId = instanceId ?? string.Empty;
            ArchetypeId = archetypeId ?? string.Empty;
            ChosenDisplayName = chosenDisplayName ?? string.Empty;
            FactionId = factionId ?? string.Empty;
        }
    }
}
