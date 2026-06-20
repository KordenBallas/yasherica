namespace Narrative.Facts.Core
{
    /// <summary>
    /// The four value types a fact can hold. Authoring (FactKeyDefinition), the typed accessor
    /// layer, and the runtime <see cref="FactValue"/> all agree on this set so values coerce
    /// predictably during comparison and effect application.
    /// </summary>
    public enum FactValueType
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3
    }
}
