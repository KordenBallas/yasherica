using System.Collections.Generic;
using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace Narrative.Actors.Data
{
    /// <summary>
    /// ScriptableObject for an NPC archetype — identity only (R1). Appearance, names, faction, base
    /// disposition, and matching tags. Deliberately carries NO dialogue, quest, enemy, or hostility
    /// data; role and hostility are properties of the casting (R3). Configuration data only.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcArchetype", menuName = "Narrative/Actors/Archetype")]
    public class NpcArchetype : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _archetypeId;
        [Tooltip("Candidate display names; the casting picks one per run")]
        [SerializeField] private List<string> _displayNamePool = new List<string>();

        [Header("Appearance")]
        [Tooltip("Modular character recipe used to spawn the visual (IModularCharacterFactory)")]
        [SerializeField] private CharacterAssemblyDefinition _assembly;
        [SerializeField] private Sprite _portrait;
        [Tooltip("Demo role tint applied over the shared model; alpha 0 = untinted (placeholder until real per-faction art)")]
        [SerializeField] private Color _demoTint = new Color(0f, 0f, 0f, 0f);

        [Header("Disposition & Matching")]
        [Tooltip("Faction id this archetype belongs to (R10) - id only, no faction asset reference")]
        [SerializeField] private string _factionId;
        [Tooltip("Personality seed; NOT hostility (hostility is a runtime fact on the casting)")]
        [SerializeField] private int _baseDisposition;
        [Tooltip("Semantic traits used to match this archetype into story roles")]
        [SerializeField] private List<string> _archetypeTags = new List<string>();

        public string ArchetypeId => _archetypeId;
        public IReadOnlyList<string> DisplayNamePool => _displayNamePool;
        public CharacterAssemblyDefinition Assembly => _assembly;
        public Sprite Portrait => _portrait;
        public Color DemoTint => _demoTint;
        public string FactionId => _factionId;
        public int BaseDisposition => _baseDisposition;
        public IReadOnlyList<string> ArchetypeTags => _archetypeTags;
    }
}
