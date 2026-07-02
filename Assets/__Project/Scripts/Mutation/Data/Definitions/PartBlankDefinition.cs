using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one Part-Blank: the socketed recipe a mutation
    /// grows from. Contains ONLY configuration data - NO logic.
    /// The blank fixes the organ (its character slot) and carries the
    /// species/passport marker; the socketed artifacts shape the ability. A
    /// designer adds a blank by creating an asset, never code.
    /// </summary>
    [CreateAssetMenu(fileName = "PartBlankDefinition", menuName = "Mutation/Part Blank")]
    public class PartBlankDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id (e.g. 'blank.skull')")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Form (what organ this blank grows)")]
        [Tooltip("The character slot the unsealed mutation installs into")]
        [SerializeField] private SlotDefinition _slot;

        [Tooltip("Species/passport marker (ArchetypeDefinition.Id, e.g. 'reptile') - the blank, not the reagents, decides what races read")]
        [SerializeField] private string _speciesArchetypeId;

        [Header("Sockets")]
        [Tooltip("How many artifacts must be socketed; filling the last socket unseals the blank")]
        [SerializeField, Min(1)] private int _socketCount = 2;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public SlotDefinition Slot => _slot;
        public string SpeciesArchetypeId => _speciesArchetypeId;
        public int SocketCount => _socketCount;
        public Sprite Icon => _icon;
    }
}
