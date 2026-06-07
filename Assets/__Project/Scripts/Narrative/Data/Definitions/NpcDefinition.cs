using UnityEngine;
using Combat.Data.Definitions;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for NPC configurations.
    /// Contains identity, visual, character Ink, and combat data.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcDefinition", menuName = "Narrative/NPCs/NPC")]
    public class NpcDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _npcId;
        [SerializeField] private string _displayName;

        [Header("Visual")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _portrait;

        [Header("Character Ink")]
        [Tooltip("Compiled Ink JSON for this NPC's personality/dialogue")]
        [SerializeField] private TextAsset _characterInkJson;

        [Tooltip("Starting knot in the character Ink file")]
        [SerializeField] private string _characterStartKnot = "greeting";

        [Header("Filters")]
        [SerializeField] private string[] _tags;
        [SerializeField] private NpcFaction _faction = NpcFaction.Neutral;

        [Header("Combat")]
        [SerializeField] private bool _canBecomeEnemy;
        [SerializeField] private EnemyDefinition _enemyDefinition;

        public string NpcId => _npcId;
        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public Sprite Portrait => _portrait;
        public TextAsset CharacterInkJson => _characterInkJson;
        public string CharacterStartKnot => _characterStartKnot;
        public System.Collections.Generic.IReadOnlyList<string> Tags => _tags;
        public NpcFaction Faction => _faction;
        public bool CanBecomeEnemy => _canBecomeEnemy;
        public EnemyDefinition EnemyDefinition => _enemyDefinition;

        public bool HasCharacterInk => _characterInkJson != null;

        public string GetCharacterInkJson()
        {
            return _characterInkJson != null ? _characterInkJson.text : string.Empty;
        }

        public bool HasTag(string tag)
        {
            if (_tags == null || _tags.Length == 0)
                return false;

            for (int i = 0; i < _tags.Length; i++)
            {
                if (_tags[i] == tag)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// NPC faction alignment.
    /// </summary>
    public enum NpcFaction
    {
        Neutral,
        Friendly,
        Hostile,
        Merchant,
        QuestGiver
    }
}
