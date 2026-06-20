namespace Narrative.Facts.Core
{
    /// <summary>
    /// Subject arity of a fact key — how many distinct subjects it is scoped to. Deliberately
    /// separate from <see cref="FactNamespace"/> so namespace stays a pure grouping label and
    /// entity-scoped world facts (e.g. <c>world.&lt;locationId&gt;.burned</c>) are expressible.
    /// </summary>
    public enum FactScope
    {
        /// <summary>One global value; subject is empty.</summary>
        Global = 0,

        /// <summary>One value per actor instance; subject is the actor instance id.</summary>
        PerActor = 1,

        /// <summary>One value per faction; subject is the faction id.</summary>
        PerFaction = 2,

        /// <summary>One value per location; subject is the location id.</summary>
        PerLocation = 3
    }
}
