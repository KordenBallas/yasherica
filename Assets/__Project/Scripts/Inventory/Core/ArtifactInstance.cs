namespace Inventory.Core
{
    /// <summary>
    /// Immutable runtime artifact owned by the inventory or a crafting session.
    /// Identity is the instance id; the definition id links to authored artifact data.
    /// </summary>
    public sealed class ArtifactInstance
    {
        public int InstanceId { get; }
        public string DefinitionId { get; }

        public ArtifactInstance(int instanceId, string definitionId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
        }
    }
}
