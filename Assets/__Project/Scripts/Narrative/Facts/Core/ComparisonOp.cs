namespace Narrative.Facts.Core
{
    /// <summary>
    /// Comparison operators a precondition can apply to a fact value. Ordered ops (Gt/Gte/Lt/Lte)
    /// are meaningful only for numeric values; Exists/NotExists test presence in the store and ignore
    /// the literal/default (B4).
    /// </summary>
    public enum ComparisonOp
    {
        Eq = 0,
        NotEq = 1,
        Gt = 2,
        Gte = 3,
        Lt = 4,
        Lte = 5,
        Exists = 6,
        NotExists = 7
    }
}
