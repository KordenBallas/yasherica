using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Builds one character from a CharacterAssemblyDefinition: instantiates the rig,
    /// validates the definitions against the live bone hierarchy (fail fast), wires the
    /// per-character object graph, and equips default parts through the normal swap path.
    /// </summary>
    public class ModularCharacterFactory : IModularCharacterFactory
    {
        private readonly IPartCatalog _partCatalog;
        private readonly IGameLogger _logger;
        private readonly AssemblyValidator _validator = new AssemblyValidator();

        public ModularCharacterFactory(IPartCatalog partCatalog, IGameLogger logger)
        {
            _partCatalog = partCatalog;
            _logger = logger;
        }

        public ModularCharacter Create(CharacterAssemblyDefinition assembly, Transform parent)
        {
            if (assembly == null || assembly.Skeleton == null || assembly.Skeleton.RigPrefab == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[ModularCharacterFactory] Assembly definition, skeleton, or rig prefab is missing.");
                return null;
            }

            if (!ValidateParts(assembly.Skeleton, assembly.Parts))
            {
                return null;
            }

            var name = string.IsNullOrEmpty(assembly.Id) ? assembly.Skeleton.Id : assembly.Id;
            var controller = BuildRigAndController(assembly.Skeleton, name, parent, out var rigObject);
            if (controller == null)
            {
                return null;
            }

            foreach (var part in assembly.Parts)
            {
                if (part != null)
                {
                    controller.SwapPart(part);
                }
            }

            foreach (var attachment in assembly.DefaultAttachments)
            {
                if (attachment != null)
                {
                    controller.AttachToSocket(attachment);
                }
            }

            var character = rigObject.AddComponent<ModularCharacter>();
            character.Initialize(controller);
            return character;
        }

        public ModularCharacter Create(
            SkeletonDefinition skeleton,
            System.Collections.Generic.IReadOnlyList<PartDefinition> activeParts,
            System.Collections.Generic.IReadOnlyList<PartDefinition> dormantParts,
            System.Collections.Generic.IReadOnlyList<AttachmentDefinition> attachments,
            Transform parent)
        {
            if (skeleton == null || skeleton.RigPrefab == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[ModularCharacterFactory] Skeleton or rig prefab is missing.");
                return null;
            }

            if (!ValidateParts(skeleton, activeParts))
            {
                return null;
            }

            var controller = BuildRigAndController(skeleton, skeleton.Id, parent, out var rigObject);
            if (controller == null)
            {
                return null;
            }

            // All-or-nothing: a frame change stages this body before touching the live one,
            // so any equip failure must abort the whole build.
            if (activeParts != null)
            {
                foreach (var part in activeParts)
                {
                    if (part != null && !controller.SwapPart(part))
                    {
                        _logger.Error(LogCategory.CharacterSystem,
                            $"[ModularCharacterFactory] Part '{part.Id}' failed to equip on skeleton '{skeleton.Id}'; aborting the staged build.");
                        controller.Dispose();
                        Object.Destroy(rigObject);
                        return null;
                    }
                }
            }

            if (dormantParts != null)
            {
                foreach (var part in dormantParts)
                {
                    if (part != null)
                    {
                        controller.EquipDormant(part);
                    }
                }
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment != null)
                    {
                        controller.AttachToSocket(attachment);
                    }
                }
            }

            var character = rigObject.AddComponent<ModularCharacter>();
            character.Initialize(controller);
            return character;
        }

        private CharacterAssemblyController BuildRigAndController(
            SkeletonDefinition skeleton, string name, Transform parent, out GameObject rigObject)
        {
            rigObject = Object.Instantiate(skeleton.RigPrefab, parent);
            rigObject.name = name;

            var rig = rigObject.GetComponent<CharacterRig>();
            if (rig == null)
            {
                _logger.Error(LogCategory.CharacterSystem,$"[ModularCharacterFactory] Rig prefab '{skeleton.RigPrefab.name}' has no CharacterRig component.");
                Object.Destroy(rigObject);
                rigObject = null;
                return null;
            }

            rig.Initialize();

            if (!ValidateLiveRig(skeleton, rig))
            {
                Object.Destroy(rigObject);
                rigObject = null;
                return null;
            }

            var controller = new CharacterAssemblyController(
                skeleton,
                _partCatalog,
                new PartSwapExecutor(rig, _logger),
                new SocketMounter(rig, _logger),
                _logger);

            controller.MountSkeletonSockets();
            return controller;
        }

        private bool ValidateParts(
            SkeletonDefinition skeletonDefinition,
            System.Collections.Generic.IReadOnlyList<PartDefinition> partDefinitions)
        {
            var skeleton = DefinitionMapper.ToSkeletonData(skeletonDefinition);
            var parts = new System.Collections.Generic.List<PartData>(partDefinitions?.Count ?? 0);
            if (partDefinitions != null)
            {
                foreach (var part in partDefinitions)
                {
                    if (part != null)
                    {
                        parts.Add(DefinitionMapper.ToPartData(part));
                    }
                }
            }

            var hasErrors = false;
            foreach (var issue in _validator.ValidateAssembly(skeleton, parts))
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    hasErrors = true;
                    _logger.Error(LogCategory.CharacterSystem,$"[ModularCharacterFactory] {issue}");
                }
                else
                {
                    _logger.Warning(LogCategory.CharacterSystem,$"[ModularCharacterFactory] {issue}");
                }
            }

            return !hasErrors;
        }

        private bool ValidateLiveRig(SkeletonDefinition skeleton, CharacterRig rig)
        {
            var valid = true;
            foreach (var boneName in skeleton.BoneNames)
            {
                if (!rig.TryGetBone(boneName, out _))
                {
                    _logger.Error(LogCategory.CharacterSystem,
                        $"[ModularCharacterFactory] SkeletonDefinition '{skeleton.Id}' lists bone '{boneName}' but the instantiated rig prefab has no such transform. Re-sync the bone list from the rig prefab.");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
