namespace Mutation.Core
{
    /// <summary>
    /// One Part-Blank held in the rack: a unique instance of an authored
    /// definition (mirrors the inventory's ArtifactInstance).
    /// </summary>
    public sealed class BlankInstance
    {
        public int InstanceId { get; }
        public string DefinitionId { get; }

        public BlankInstance(int instanceId, string definitionId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
        }
    }
}
