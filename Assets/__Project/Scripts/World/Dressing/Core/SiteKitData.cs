using System;
using System.Collections.Generic;

namespace World.Dressing.Core
{
    /// <summary>
    /// The pure shape of one site-dressing kit (site-camp-dressing brief FR1–FR2): how many entries
    /// each role list holds plus the entries' scale ranges. The role lists' prefabs live on the kit
    /// asset, index-aligned per role — the planner picks role + index, the spawner resolves the mesh.
    /// </summary>
    public sealed class SiteKitData
    {
        public SiteKitData(
            string dressingThemeId,
            int structureCount,
            int propCount,
            int focalCount,
            int gateCount)
        {
            DressingThemeId = dressingThemeId ?? string.Empty;
            StructureCount = Math.Max(0, structureCount);
            PropCount = Math.Max(0, propCount);
            FocalCount = Math.Max(0, focalCount);
            GateCount = Math.Max(0, gateCount);
        }

        /// <summary>Matched against <c>SiteStamp.DressingThemeId</c> (e.g. "settlement-kit").</summary>
        public string DressingThemeId { get; }

        /// <summary>Skyline structures (houses) — blocking, whole-cell.</summary>
        public int StructureCount { get; }

        /// <summary>Small decorative props (sacks / barrels / crates) — multi-per-cell.</summary>
        public int PropCount { get; }

        /// <summary>The focal cluster (the camp's fire) — one blocking cell, placed together.</summary>
        public int FocalCount { get; }

        /// <summary>Threshold pieces marking arrival — placed on the block's anchor platform only.</summary>
        public int GateCount { get; }

        public bool IsEmpty => StructureCount == 0 && PropCount == 0 && FocalCount == 0 && GateCount == 0;
    }
}
