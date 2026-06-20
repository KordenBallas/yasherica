namespace Narrative.Facts.Core
{
    /// <summary>
    /// Mutation a fact effect applies to the store. Not all ops are valid for every
    /// <see cref="FactValueType"/> — the applier enforces the legal op×type matrix (B5):
    /// Set is universal; Add is numeric (Int/Float); Toggle is Bool only; Remove is universal.
    /// </summary>
    public enum FactEffectOp
    {
        Set = 0,
        Add = 1,
        Toggle = 2,
        Remove = 3
    }
}
