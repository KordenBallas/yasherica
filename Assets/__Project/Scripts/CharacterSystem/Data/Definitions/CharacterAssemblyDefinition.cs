using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A complete character recipe: skeleton, one part per slot,
    /// and optional default attachments applied after assembly.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterAssemblyDefinition", menuName = "Character System/Character Assembly")]
    public class CharacterAssemblyDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private SkeletonDefinition _skeleton;
        [SerializeField] private List<PartDefinition> _parts;
        [SerializeField] private List<AttachmentDefinition> _defaultAttachments;

        public string Id => _id;
        public SkeletonDefinition Skeleton => _skeleton;
        public IReadOnlyList<PartDefinition> Parts => _parts ?? (IReadOnlyList<PartDefinition>)System.Array.Empty<PartDefinition>();
        public IReadOnlyList<AttachmentDefinition> DefaultAttachments => _defaultAttachments ?? (IReadOnlyList<AttachmentDefinition>)System.Array.Empty<AttachmentDefinition>();
    }
}
