using System;
using Narrative.Facts.Core;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// Authoring form of a fact effect: "&lt;namespace&gt;.&lt;subjectToken&gt;.&lt;key&gt; &lt;op&gt;
    /// &lt;value&gt;". Same shape as <see cref="FactPredicateSerial"/> but with a mutation
    /// <see cref="FactEffectOp"/>. Maps to the UnityEngine-free <see cref="FactEffectCore"/>.
    /// </summary>
    [Serializable]
    public class FactEffectSerial
    {
        [SerializeField] private FactNamespace _namespace = FactNamespace.World;
        [Tooltip("Empty for global facts, or a context token like $self / $target / $faction / $location")]
        [SerializeField] private string _subjectToken = string.Empty;
        [SerializeField] private string _key = string.Empty;
        [SerializeField] private FactEffectOp _op = FactEffectOp.Set;
        [SerializeField] private FactValueAuthoring _value = new FactValueAuthoring();

        public FactEffectSerial()
        {
        }

        public FactEffectSerial(FactNamespace ns, string subjectToken, string key, FactEffectOp op, FactValueAuthoring value)
        {
            _namespace = ns;
            _subjectToken = subjectToken;
            _key = key;
            _op = op;
            _value = value;
        }

        public FactEffectCore ToCore() =>
            new FactEffectCore(_namespace, _subjectToken, _key, _op,
                (_value ?? new FactValueAuthoring()).ToCore());

        /// <summary>The permitted-write shape this effect occupies (for footprint declarations).</summary>
        public FactKeyShapeCore ToShape() => ToCore().Shape;
    }
}
