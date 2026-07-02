namespace Inventory.Core
{
    /// <summary>
    /// One (artifact definition id, trait profile) pair exposed by the trait source.
    /// </summary>
    public readonly struct ArtifactTraitEntry
    {
        public string DefinitionId { get; }
        public ArtifactTraitProfile Profile { get; }

        public ArtifactTraitEntry(string definitionId, ArtifactTraitProfile profile)
        {
            DefinitionId = definitionId;
            Profile = profile;
        }
    }
}
