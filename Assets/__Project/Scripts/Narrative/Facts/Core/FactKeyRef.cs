namespace Narrative.Facts.Core
{
    /// <summary>
    /// Compile-safe handle to a declared fact key (the typed-accessor hybrid layer, R13). Curated
    /// constants in <c>WorldFacts</c>/<c>ActorFacts</c> mirror a subset of the SO
    /// registry, which stays authoritative; a startup check (<see cref="FactKeyRefRegistryCheck"/>)
    /// asserts every ref exists. Holds the namespace, scope, bare key, and value type so the typed
    /// store extensions can build a <see cref="FactKey"/> and pick the right default.
    /// </summary>
    public readonly struct FactKeyRef
    {
        public FactNamespace Namespace { get; }
        public FactScope Scope { get; }
        public string Key { get; }
        public FactValueType ValueType { get; }

        public FactKeyRef(FactNamespace ns, FactScope scope, string key, FactValueType valueType)
        {
            Namespace = ns;
            Scope = scope;
            Key = key;
            ValueType = valueType;
        }

        /// <summary>Builds the concrete store key for a given subject (empty for global facts).</summary>
        public FactKey ToKey(string subject = "") => new FactKey(Namespace, subject, Key);
    }
}
