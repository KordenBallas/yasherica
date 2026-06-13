using CharacterSystem.Core;
using Core.Logging;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Executes one Animator-state-safe part swap: instantiates the part prefab under the
    /// rig root, remaps its SkinnedMeshRenderer bones onto the shared skeleton by name,
    /// and removes the previous instance. Never touches the Animator, so the playing
    /// animation continues uninterrupted.
    /// </summary>
    public class PartSwapExecutor
    {
        private readonly ICharacterRig _rig;
        private readonly IGameLogger _logger;

        public PartSwapExecutor(ICharacterRig rig, IGameLogger logger)
        {
            _rig = rig;
            _logger = logger;
        }

        public GameObject Swap(PartData part, GameObject partPrefab, GameObject oldInstance)
        {
            if (partPrefab == null)
            {
                _logger.Error($"[PartSwapExecutor] Part '{part.PartId}' has no prefab assigned.");
                return null;
            }

            var bones = new Transform[part.BoneNames.Count];
            for (var i = 0; i < part.BoneNames.Count; i++)
            {
                if (!_rig.TryGetBone(part.BoneNames[i], out bones[i]))
                {
                    _logger.Error(
                        $"[PartSwapExecutor] Part '{part.PartId}' needs bone '{part.BoneNames[i]}' which is missing on the rig. Swap aborted.");
                    return null;
                }
            }

            var instance = Object.Instantiate(partPrefab, _rig.Root, false);
            instance.name = part.PartId;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                _logger.Error($"[PartSwapExecutor] Part prefab '{partPrefab.name}' contains no SkinnedMeshRenderer. Swap aborted.");
                Object.Destroy(instance);
                return null;
            }

            // bones[i] must correspond to sharedMesh.bindposes[i]; PartDefinition bone
            // lists are baked in mesh-bone-index order to guarantee this.
            renderer.bones = bones;
            renderer.rootBone = _rig.RootBone;

            if (oldInstance != null)
            {
                // Hide before Destroy: destruction is deferred to end of frame and the
                // new renderer is already live, so neither a gap nor double geometry shows.
                var oldRenderer = oldInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (oldRenderer != null)
                {
                    oldRenderer.enabled = false;
                }

                Object.Destroy(oldInstance);
            }

            return instance;
        }
    }
}
