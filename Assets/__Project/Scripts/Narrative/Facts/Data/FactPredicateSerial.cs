using System;
using Narrative.Facts.Core;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// Authoring form of a precondition predicate: "&lt;namespace&gt;.&lt;subjectToken&gt;.&lt;key&gt;
    /// &lt;op&gt; &lt;value&gt;". The subject token is an open string — empty for global facts, or a
    /// context binding such as <c>$self</c>/<c>$target</c>/<c>$faction</c>/<c>$location</c>. Maps to
    /// the UnityEngine-free <see cref="FactPredicate"/> via <see cref="ToCore"/>.
    /// </summary>
    [Serializable]
    public class FactPredicateSerial
    {
        [SerializeField] private FactNamespace _namespace = FactNamespace.World;
        [Tooltip("Empty for global facts, or a context token like $self / $target / $faction / $location")]
        [SerializeField] private string _subjectToken = string.Empty;
        [SerializeField] private string _key = string.Empty;
        [SerializeField] private ComparisonOp _op = ComparisonOp.Eq;
        [SerializeField] private FactValueAuthoring _value = new FactValueAuthoring();

        public FactPredicateSerial()
        {
        }

        public FactPredicateSerial(FactNamespace ns, string subjectToken, string key, ComparisonOp op, FactValueAuthoring value)
        {
            _namespace = ns;
            _subjectToken = subjectToken;
            _key = key;
            _op = op;
            _value = value;
        }

        public FactPredicate ToCore() =>
            new FactPredicate(_namespace, _subjectToken, _key, _op,
                (_value ?? new FactValueAuthoring()).ToCore());
    }
}
