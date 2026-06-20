namespace Narrative.Facts.Core
{
    /// <summary>
    /// Read-only view of the authored fact vocabulary (the single source of truth, R13). The fact
    /// store consults it to fail closed on unknown/mistyped keys; the precondition evaluator uses it
    /// for the per-key default that participates in comparisons (B4). The concrete implementation is
    /// mapped from the <c>FactKeyRegistry</c> ScriptableObject at install time.
    /// </summary>
    public interface IFactKeyRegistry
    {
        /// <summary>True if a key with this namespace + bare key name is declared in the vocabulary.</summary>
        bool TryGetInfo(FactNamespace ns, string key, out FactKeyInfo info);
    }

    /// <summary>Immutable description of one declared fact key.</summary>
    public readonly struct FactKeyInfo
    {
        public FactNamespace Namespace { get; }
        public string Key { get; }
        public FactScope Scope { get; }
        public FactValueType ValueType { get; }
        public FactValue DefaultValue { get; }

        public FactKeyInfo(FactNamespace ns, string key, FactScope scope, FactValueType valueType, FactValue defaultValue)
        {
            Namespace = ns;
            Key = key ?? string.Empty;
            Scope = scope;
            ValueType = valueType;
            DefaultValue = defaultValue;
        }
    }
}
