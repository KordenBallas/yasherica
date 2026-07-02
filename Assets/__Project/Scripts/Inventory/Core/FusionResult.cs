namespace Inventory.Core
{
    /// <summary>
    /// Outcome of resolving a combine: the artifact produced and whether it came
    /// from an authored signature recipe (vs the emergent grammar).
    /// </summary>
    public readonly struct FusionResult
    {
        public string OutputDefinitionId { get; }
        public bool IsSignature { get; }

        public FusionResult(string outputDefinitionId, bool isSignature)
        {
            OutputDefinitionId = outputDefinitionId;
            IsSignature = isSignature;
        }
    }
}
