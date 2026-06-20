namespace Narrative.Facts.Core
{
    /// <summary>
    /// Grouping label for a fact key. This is a vocabulary/organisation axis only:
    /// subject arity (how many subjects a key is scoped to) comes from <see cref="FactScope"/>,
    /// NOT from the namespace. A single unified store spans all namespaces (Narrative R9), so the
    /// precondition/effect machinery operates uniformly across them.
    /// </summary>
    public enum FactNamespace
    {
        /// <summary>World events / locations (e.g. <c>world.pass_cleared</c>).</summary>
        World = 0,

        /// <summary>Per-NPC state (e.g. <c>actor.&lt;instanceId&gt;.hostile</c>).</summary>
        Actor = 1,

        /// <summary>Faction reputation / power (e.g. <c>faction.&lt;factionId&gt;.reputation</c>).</summary>
        Faction = 2
    }
}
