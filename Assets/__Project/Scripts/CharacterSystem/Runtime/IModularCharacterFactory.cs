using System.Collections.Generic;
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

        /// <summary>
        /// Builds a body on an explicit skeleton from explicit part lists — the body-plan
        /// change path. Stricter than the assembly path: if ANY active part fails to equip
        /// the whole build is aborted and null is returned, so a frame change can stage the
        /// new body fully before touching the live one (all-or-nothing).
        /// </summary>
        ModularCharacter Create(
            SkeletonDefinition skeleton,
            IReadOnlyList<PartDefinition> activeParts,
            IReadOnlyList<PartDefinition> dormantParts,
            IReadOnlyList<AttachmentDefinition> attachments,
            Transform parent);
    }
}
