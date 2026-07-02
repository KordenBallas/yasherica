using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Display data for one ability a body part grants, as the mutation card shows it:
    /// name + description (the hover tooltip) and icon (the card face row). Lives in the
    /// Data layer so the presenter/view never touch the Combat ability ScriptableObjects.
    /// </summary>
    public readonly struct MutationAbilityInfo
    {
        public string Name { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool IsPassive { get; }

        public MutationAbilityInfo(string name, string description, Sprite icon, bool isPassive)
        {
            Name = name;
            Description = description;
            Icon = icon;
            IsPassive = isPassive;
        }
    }
}
