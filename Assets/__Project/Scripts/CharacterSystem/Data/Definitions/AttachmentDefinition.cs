using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A static prop (weapon, hat, shield, ...) that attaches to a socket
    /// with an extra local offset on top of the socket's own TRS.
    /// </summary>
    [CreateAssetMenu(fileName = "AttachmentDefinition", menuName = "Character System/Attachment")]
    public class AttachmentDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private string _socketId;
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localRotationEuler;
        [SerializeField] private Vector3 _localScale = Vector3.one;

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public string SocketId => _socketId;
        public Vector3 LocalPosition => _localPosition;
        public Quaternion LocalRotation => Quaternion.Euler(_localRotationEuler);
        public Vector3 LocalScale => _localScale;
    }
}
