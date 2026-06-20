using System;
using Narrative.Facts.Core;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// Authoring holder for a typed fact literal. Carries all four backing fields plus the active
    /// <see cref="FactValueType"/>; the inactive fields are ignored. Pure data — the typing rules
    /// live in <see cref="FactValueConversion"/>.
    /// </summary>
    [Serializable]
    public class FactValueAuthoring
    {
        [SerializeField] private FactValueType _valueType = FactValueType.Bool;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private string _stringValue = string.Empty;

        public FactValueType ValueType => _valueType;

        // Kept for Unity serialization.
        public FactValueAuthoring()
        {
        }

        // Convenience ctor for programmatic creation and tests.
        public FactValueAuthoring(FactValueType valueType, bool boolValue = false, int intValue = 0,
            float floatValue = 0f, string stringValue = "")
        {
            _valueType = valueType;
            _boolValue = boolValue;
            _intValue = intValue;
            _floatValue = floatValue;
            _stringValue = stringValue;
        }

        public FactValue ToCore() =>
            FactValueConversion.Build(_valueType, _boolValue, _intValue, _floatValue, _stringValue);
    }
}
