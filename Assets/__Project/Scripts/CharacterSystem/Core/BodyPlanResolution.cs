namespace CharacterSystem.Core
{
    /// <summary>
    /// Result of resolving which body plan governs an equipped set:
    /// the governing skeleton and the frame-changing part that pulled it in
    /// (null when the base plan governs).
    /// </summary>
    public sealed class BodyPlanResolution
    {
        public string GoverningSkeletonId { get; }

        /// <summary>Id of the winning frame-changing part; null when no equipped part
        /// governs and the base skeleton applies.</summary>
        public string GoverningPartId { get; }

        public bool IsBasePlan => GoverningPartId == null;

        public BodyPlanResolution(string governingSkeletonId, string governingPartId)
        {
            GoverningSkeletonId = governingSkeletonId;
            GoverningPartId = governingPartId;
        }
    }
}
