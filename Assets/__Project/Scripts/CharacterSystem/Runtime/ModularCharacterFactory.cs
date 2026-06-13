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
                _logger.Error("[ModularCharacterFactory] Assembly definition, skeleton, or rig prefab is missing.");
                return null;
            }

            if (!ValidateAssembly(assembly))
            {
                return null;
            }

            var rigObject = Object.Instantiate(assembly.Skeleton.RigPrefab, parent);
            rigObject.name = string.IsNullOrEmpty(assembly.Id) ? assembly.Skeleton.Id : assembly.Id;

            var rig = rigObject.GetComponent<CharacterRig>();
            if (rig == null)
            {
                _logger.Error($"[ModularCharacterFactory] Rig prefab '{assembly.Skeleton.RigPrefab.name}' has no CharacterRig component.");
                Object.Destroy(rigObject);
                return null;
            }

            rig.Initialize();

            if (!ValidateLiveRig(assembly.Skeleton, rig))
            {
                Object.Destroy(rigObject);
                return null;
            }

            var controller = new CharacterAssemblyController(
                assembly.Skeleton,
                _partCatalog,
                new PartSwapExecutor(rig, _logger),
                new SocketMounter(rig, _logger),
                _logger);

            controller.MountSkeletonSockets();

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

        private bool ValidateAssembly(CharacterAssemblyDefinition assembly)
        {
            var skeleton = DefinitionMapper.ToSkeletonData(assembly.Skeleton);
            var parts = new System.Collections.Generic.List<PartData>(assembly.Parts.Count);
            foreach (var part in assembly.Parts)
            {
                if (part != null)
                {
                    parts.Add(DefinitionMapper.ToPartData(part));
                }
            }

            var hasErrors = false;
            foreach (var issue in _validator.ValidateAssembly(skeleton, parts))
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    hasErrors = true;
                    _logger.Error($"[ModularCharacterFactory] {issue}");
                }
                else
                {
                    _logger.Warning($"[ModularCharacterFactory] {issue}");
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
                    _logger.Error(
                        $"[ModularCharacterFactory] SkeletonDefinition '{skeleton.Id}' lists bone '{boneName}' but the instantiated rig prefab has no such transform. Re-sync the bone list from the rig prefab.");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
