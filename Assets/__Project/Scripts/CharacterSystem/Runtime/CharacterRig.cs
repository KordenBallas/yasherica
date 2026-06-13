using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Thin bone registry living on the rig prefab root. Caches bone transforms by name
    /// in <see cref="Initialize"/>, which the factory calls BEFORE any parts or sockets
    /// are added — at that moment every descendant transform is a bone.
    /// </summary>
    public class CharacterRig : MonoBehaviour, ICharacterRig
    {
        [SerializeField] private Transform _rootBone;
        [SerializeField] private Animator _animator;

        private Dictionary<string, Transform> _bonesByName;

        public Transform Root => transform;
        public Transform RootBone => _rootBone;
        public Animator Animator => _animator;

        public bool IsInitialized => _bonesByName != null;

        public void Initialize()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_rootBone == null && transform.childCount > 0)
            {
                _rootBone = transform.GetChild(0);
            }

            _bonesByName = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            CollectBonesRecursive(_rootBone);
        }

        public bool TryGetBone(string boneName, out Transform bone)
        {
            if (_bonesByName == null || boneName == null)
            {
                bone = null;
                return false;
            }

            return _bonesByName.TryGetValue(boneName, out bone);
        }

        private void CollectBonesRecursive(Transform current)
        {
            if (current == null)
            {
                return;
            }

            // Last registration wins is unacceptable for bones: keep the first and let
            // AssemblyValidator surface duplicate names as an authoring issue.
            if (!_bonesByName.ContainsKey(current.name))
            {
                _bonesByName.Add(current.name, current);
            }

            for (var i = 0; i < current.childCount; i++)
            {
                CollectBonesRecursive(current.GetChild(i));
            }
        }
    }
}
