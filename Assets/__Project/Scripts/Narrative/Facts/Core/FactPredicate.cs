namespace Narrative.Facts.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free precondition: a single predicate over one fact —
    /// "&lt;namespace&gt;.&lt;subjectToken&gt;.&lt;key&gt; &lt;op&gt; &lt;value&gt;". The subject token is
    /// unresolved here (e.g. <c>$self</c>, <c>$location</c>, or empty for global); the evaluator
    /// resolves it against a casting context before reading the store. Produced from authored data
    /// via <c>FactPredicateSerial.ToCore()</c>.
    /// </summary>
    public sealed class FactPredicate
    {
        public FactNamespace Namespace { get; }
        public string SubjectToken { get; }
        public string Key { get; }
        public ComparisonOp Op { get; }
        public FactValue Value { get; }

        public FactPredicate(FactNamespace ns, string subjectToken, string key, ComparisonOp op, FactValue value)
        {
            Namespace = ns;
            SubjectToken = subjectToken ?? string.Empty;
            Key = key ?? string.Empty;
            Op = op;
            Value = value;
        }
    }
}
