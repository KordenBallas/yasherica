using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// ScriptableObject mapping one creature archetype to the body-part options it can grant at a
    /// stage-up mutation. Configuration data only - NO logic. One asset per archetype (mirrors the
    /// <see cref="ArchetypeDefinition"/> convention), loaded from Resources/Mutation/PartSets by the
    /// MutationInstaller. Parts are referenced by id string and resolved through the character part
    /// catalog, so this asset carries no hard reference into the Character System.
    /// </summary>
    [CreateAssetMenu(fileName = "ArchetypePartSetDefinition", menuName = "Mutation/Archetype Part Set")]
    public class ArchetypePartSetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ArchetypeDefinition.Id whose dominance offers these parts (e.g. 'reptile')")]
        [SerializeField] private string _archetypeId;

        [Header("Body-part options")]
        [Tooltip("Parts this archetype can offer at a stage-up mutation, in the order they are tried")]
        [SerializeField] private List<MutationOptionEntry> _options = new List<MutationOptionEntry>();

        public string ArchetypeId => _archetypeId;
        public IReadOnlyList<MutationOptionEntry> Options =>
            _options ?? (IReadOnlyList<MutationOptionEntry>)Array.Empty<MutationOptionEntry>();
    }

    /// <summary>
    /// One authored body-part option an archetype can grant: the slot it fills and the part id that
    /// fills it (both resolved through the character system), plus a label and icon for the choice UI.
    /// Configuration data only.
    /// </summary>
    [Serializable]
    public class MutationOptionEntry
    {
        [Tooltip("SlotDefinition.Id the part fills (e.g. 'slot.head')")]
        [SerializeField] private string _slotId;

        [Tooltip("PartDefinition.Id swapped in when this option is chosen (e.g. 'part.head.b')")]
        [SerializeField] private string _partId;

        [Tooltip("Label shown on the mutation choice button")]
        [SerializeField] private string _displayName;

        [Tooltip("Optional icon shown on the mutation choice button")]
        [SerializeField] private Sprite _icon;

        public string SlotId => _slotId;
        public string PartId => _partId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;

        // Parameterless ctor kept for Unity serialization.
        public MutationOptionEntry()
        {
        }

        // Convenience ctor for programmatic creation and tests.
        public MutationOptionEntry(string slotId, string partId, string displayName, Sprite icon = null)
        {
            _slotId = slotId;
            _partId = partId;
            _displayName = displayName;
            _icon = icon;
        }
    }
}
