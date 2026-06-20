using System;
using System.Globalization;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Immutable tagged-union value for the fact store. Holds exactly one of bool/int/float/string,
    /// tagged by <see cref="Type"/>. Pure C#, UnityEngine-free.
    ///
    /// Comparison (<see cref="CompareNumericOrEquatable"/>) is used by the precondition evaluator:
    /// numeric types (Bool/Int/Float) compare by their numeric value so an authored <c>Int</c>
    /// literal can be compared against a stored <c>Float</c>; strings compare by ordinal equality only.
    /// </summary>
    public readonly struct FactValue : IEquatable<FactValue>
    {
        public FactValueType Type { get; }

        private readonly bool _bool;
        private readonly long _int;
        private readonly double _float;
        private readonly string _string;

        private FactValue(FactValueType type, bool b, long i, double f, string s)
        {
            Type = type;
            _bool = b;
            _int = i;
            _float = f;
            _string = s;
        }

        public static FactValue FromBool(bool value) => new FactValue(FactValueType.Bool, value, 0, 0, null);
        public static FactValue FromInt(long value) => new FactValue(FactValueType.Int, false, value, 0, null);
        public static FactValue FromFloat(double value) => new FactValue(FactValueType.Float, false, 0, value, null);
        public static FactValue FromString(string value) => new FactValue(FactValueType.String, false, 0, 0, value ?? string.Empty);

        /// <summary>The type-appropriate zero/empty value, used as an unset-fact default fallback.</summary>
        public static FactValue DefaultFor(FactValueType type)
        {
            switch (type)
            {
                case FactValueType.Bool: return FromBool(false);
                case FactValueType.Int: return FromInt(0);
                case FactValueType.Float: return FromFloat(0d);
                case FactValueType.String: return FromString(string.Empty);
                default: return FromBool(false);
            }
        }

        public bool AsBool() => Type == FactValueType.Bool ? _bool : NumericValue() != 0d;
        public long AsInt() => Type == FactValueType.Int ? _int : (long)NumericValue();
        public double AsFloat() => NumericValue();
        public string AsString() => Type == FactValueType.String ? (_string ?? string.Empty) : ToString();

        /// <summary>True for Bool/Int/Float; false for String.</summary>
        public bool IsNumeric => Type != FactValueType.String;

        private double NumericValue()
        {
            switch (Type)
            {
                case FactValueType.Bool: return _bool ? 1d : 0d;
                case FactValueType.Int: return _int;
                case FactValueType.Float: return _float;
                default: return 0d;
            }
        }

        /// <summary>
        /// Three-way comparison used by ordered ops (Gt/Gte/Lt/Lte) and equality. Numeric values
        /// compare numerically across Bool/Int/Float; if either side is a string both are compared
        /// by ordinal string equality (returning 0 for equal, non-zero otherwise — ordering of
        /// strings is intentionally undefined for Gt/Lt and the evaluator only treats == / != for them).
        /// </summary>
        public int CompareNumericOrEquatable(FactValue other)
        {
            if (IsNumeric && other.IsNumeric)
            {
                return NumericValue().CompareTo(other.NumericValue());
            }

            return string.Equals(AsString(), other.AsString(), StringComparison.Ordinal) ? 0 : 1;
        }

        public bool Equals(FactValue other)
        {
            if (IsNumeric && other.IsNumeric)
            {
                return NumericValue() == other.NumericValue();
            }

            if (!IsNumeric && !other.IsNumeric)
            {
                return string.Equals(_string ?? string.Empty, other._string ?? string.Empty, StringComparison.Ordinal);
            }

            return false;
        }

        public override bool Equals(object obj) => obj is FactValue other && Equals(other);

        public override int GetHashCode()
        {
            // Hash by canonical form: numeric values share a numeric hash, strings hash ordinally.
            return IsNumeric ? NumericValue().GetHashCode() : (_string ?? string.Empty).GetHashCode();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case FactValueType.Bool: return _bool ? "true" : "false";
                case FactValueType.Int: return _int.ToString(CultureInfo.InvariantCulture);
                case FactValueType.Float: return _float.ToString(CultureInfo.InvariantCulture);
                default: return _string ?? string.Empty;
            }
        }
    }
}
