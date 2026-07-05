using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace World.Biomes.Data
{
    /// <summary>
    /// The biome journey's authored roster: which biomes are in the run rotation, their escalation
    /// tier (ordering key only — nothing scales difficulty off it yet), selection weight within the
    /// tier, and stretch length in planning windows. Configuration data only — the journey consumes
    /// the mapped Core <c>BiomeProgressionSettings</c>, never this SO. A biome with no entry here (or
    /// weight 0) never appears: that is the data-driven exclusion (Cave today).
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeProgressionConfig", menuName = "World/Biome Progression")]
    public class BiomeProgressionConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The biome this entry puts into the rotation")]
            [SerializeField] private LevelTheme _theme;
            [Tooltip("Escalation tier (ordering key): the run climbs from the lowest authored tier upward")]
            [Min(1)]
            [SerializeField] private int _escalationTier = 1;
            [Tooltip("Weight of this biome within its tier's seeded pick; 0 excludes it without deleting the entry")]
            [Min(0)]
            [SerializeField] private int _selectionWeight = 1;
            [Tooltip("Minimum stretch length, in planning windows, the run stays in this biome")]
            [Min(1)]
            [SerializeField] private int _stretchMinWindows = 3;
            [Tooltip("Maximum stretch length, in planning windows")]
            [Min(1)]
            [SerializeField] private int _stretchMaxWindows = 4;

            public LevelTheme Theme => _theme;
            public int EscalationTier => _escalationTier;
            public int SelectionWeight => _selectionWeight;
            public int StretchMinWindows => _stretchMinWindows;
            public int StretchMaxWindows => _stretchMaxWindows;
        }

        [Tooltip("One entry per biome in the run rotation")]
        [SerializeField] private List<Entry> _biomes = new List<Entry>();

        public IReadOnlyList<Entry> Biomes => _biomes;
    }
}
