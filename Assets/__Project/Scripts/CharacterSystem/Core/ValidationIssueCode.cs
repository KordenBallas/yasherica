namespace CharacterSystem.Core
{
    /// <summary>
    /// Stable issue codes so tests and tooling can react to a rule
    /// without parsing human-readable messages.
    /// </summary>
    public enum ValidationIssueCode
    {
        MissingBone,
        EmptyBoneList,
        SkeletonMismatch,
        DuplicateSocketId,
        SocketParentBoneMissing,
        DuplicateSlot,
        DuplicateBoneName
    }
}
