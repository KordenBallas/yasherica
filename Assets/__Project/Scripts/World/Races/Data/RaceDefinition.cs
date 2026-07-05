using LevelGeneration;
using UnityEngine;

namespace World.Races.Data
{
    /// <summary>
    /// One race of the world (race-roster-and-passport.md): its id (the tag body parts carry and
    /// the passport-fact subject), display name, home biome, and belonging colour (the quest /
    /// mutation card hue grammar — no consumer until the card treatment pass, P1-6). Contains ONLY
    /// configuration data — NO logic. Adding a race = authoring one of these assets under
    /// Resources/World/Races/; no code changes (FR3).
    /// </summary>
    [CreateAssetMenu(fileName = "Race_", menuName = "World/Race")]
    public class RaceDefinition : ScriptableObject
    {
        [Tooltip("Stable id used as the part race-tag and the reads_as_tier fact subject (e.g. 'ibex'). Lowercase, no spaces.")]
        [SerializeField] private string _raceId;

        [Tooltip("Name shown in UI (e.g. 'Ibex-folk').")]
        [SerializeField] private string _displayName;

        [Tooltip("The biome this race calls home (Ibex=Mountain, Lizard=Desert, Fox=Forest; Cave has no race yet).")]
        [SerializeField] private LevelTheme _homeBiome;

        [Tooltip("Belonging colour reused by the quest/mutation card grammar (archetype/belonging = hue).")]
        [SerializeField] private Color _belongingColor = Color.white;

        public string RaceId => _raceId;
        public string DisplayName => _displayName;
        public LevelTheme HomeBiome => _homeBiome;
        public Color BelongingColor => _belongingColor;
    }
}
