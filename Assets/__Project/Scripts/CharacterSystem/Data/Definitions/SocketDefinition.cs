using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A named attachment point parented to a skeleton bone with a local TRS offset.
    /// Used both for Tier-1 (skeleton) sockets and Tier-2 (part-contributed) sockets.
    /// </summary>
    [CreateAssetMenu(fileName = "SocketDefinition", menuName = "Character System/Socket")]
    public class SocketDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _parentBoneName;
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localRotationEuler;
        [SerializeField] private Vector3 _localScale = Vector3.one;

        public string Id => _id;
        public string ParentBoneName => _parentBoneName;
        public Vector3 LocalPosition => _localPosition;
        public Quaternion LocalRotation => Quaternion.Euler(_localRotationEuler);
        public Vector3 LocalScale => _localScale;
    }
}
