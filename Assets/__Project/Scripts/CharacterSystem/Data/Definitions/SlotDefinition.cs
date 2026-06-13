using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A named body-part slot (e.g. "slot.head"). Slots are data-driven:
    /// adding a new slot is just creating another asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotDefinition", menuName = "Character System/Slot")]
    public class SlotDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        public string Id => _id;
        public string DisplayName => _displayName;
    }
}
