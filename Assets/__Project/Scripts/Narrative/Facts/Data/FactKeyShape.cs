using System;
using Narrative.Facts.Core;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// Authoring form of a permitted-write *target* (W3-3): namespace + unresolved subject token +
    /// key + value type, with no op/value. Used for a fragment's declared write footprint
    /// (e.g. <c>DialogueDefinition._declaredFactWrites</c>). Maps to <see cref="FactKeyShapeCore"/>.
    /// </summary>
    [Serializable]
    public class FactKeyShape
    {
        [SerializeField] private FactNamespace _namespace = FactNamespace.World;
        [Tooltip("Empty for global facts, or a context token like $self / $target / $faction / $location")]
        [SerializeField] private string _subjectToken = string.Empty;
        [SerializeField] private string _key = string.Empty;
        [SerializeField] private FactValueType _valueType = FactValueType.Bool;

        public FactKeyShape()
        {
        }

        public FactKeyShape(FactNamespace ns, string subjectToken, string key, FactValueType valueType)
        {
            _namespace = ns;
            _subjectToken = subjectToken;
            _key = key;
            _valueType = valueType;
        }

        public FactKeyShapeCore ToCore() => new FactKeyShapeCore(_namespace, _subjectToken, _key, _valueType);
    }
}
