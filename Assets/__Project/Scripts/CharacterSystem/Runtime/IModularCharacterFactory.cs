using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    public interface IModularCharacterFactory
    {
        /// <summary>
        /// Instantiates the skeleton rig, equips the assembly's parts, applies default
        /// attachments, and returns the character facade. Returns null (with logged
        /// errors) when the definition fails validation.
        /// </summary>
        ModularCharacter Create(CharacterAssemblyDefinition assembly, Transform parent);
    }
}
