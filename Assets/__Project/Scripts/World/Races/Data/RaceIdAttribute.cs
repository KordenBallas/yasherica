using UnityEngine;

namespace World.Races.Data
{
    /// <summary>
    /// Marks a string field as a race id so the inspector draws it as a drop-down populated from
    /// the authored RaceDefinition assets (plus "(kindless)" for empty). A drop-down over data —
    /// not an enum — keeps "add a race" a pure data change (race-roster-and-passport.md FR3).
    /// </summary>
    public sealed class RaceIdAttribute : PropertyAttribute
    {
    }
}
