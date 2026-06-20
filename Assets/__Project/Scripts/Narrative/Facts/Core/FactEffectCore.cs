namespace Narrative.Facts.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free effect: a single write to one fact —
    /// "&lt;namespace&gt;.&lt;subjectToken&gt;.&lt;key&gt; &lt;op&gt; &lt;value&gt;". The subject token is
    /// unresolved (see <see cref="FactPredicate"/>). Produced from authored data via
    /// <c>FactEffectSerial.ToCore()</c>, or parsed from an Ink <c>fact:</c> tag at play-time.
    /// Its <see cref="Shape"/> is what the footprint guard validates against.
    /// </summary>
    public sealed class FactEffectCore
    {
        public FactNamespace Namespace { get; }
        public string SubjectToken { get; }
        public string Key { get; }
        public FactEffectOp Op { get; }
        public FactValue Value { get; }

        public FactEffectCore(FactNamespace ns, string subjectToken, string key, FactEffectOp op, FactValue value)
        {
            Namespace = ns;
            SubjectToken = subjectToken ?? string.Empty;
            Key = key ?? string.Empty;
            Op = op;
            Value = value;
        }

        /// <summary>The permitted-write target shape this effect occupies (namespace + token + key + type).</summary>
        public FactKeyShapeCore Shape => new FactKeyShapeCore(Namespace, SubjectToken, Key, Value.Type);
    }
}
