using System.Globalization;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Pure, UnityEngine-free construction of a <see cref="FactValue"/> from authored primitive
    /// fields, and from the textual form an Ink <c>fact:</c> tag carries. Shared by the Data-layer
    /// authoring types and the dialogue tag parser so the typing rules live in exactly one place.
    /// </summary>
    public static class FactValueConversion
    {
        /// <summary>Builds a value of the requested type from the four typed backing fields.</summary>
        public static FactValue Build(FactValueType type, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            switch (type)
            {
                case FactValueType.Bool: return FactValue.FromBool(boolValue);
                case FactValueType.Int: return FactValue.FromInt(intValue);
                case FactValueType.Float: return FactValue.FromFloat(floatValue);
                case FactValueType.String: return FactValue.FromString(stringValue);
                default: return FactValue.FromBool(boolValue);
            }
        }

        /// <summary>
        /// Parses a literal of the given type from text (Ink tag values, debug input). Returns false
        /// on a malformed numeric/bool literal so callers can fail closed; String always succeeds.
        /// </summary>
        public static bool TryParse(FactValueType type, string text, out FactValue value)
        {
            text = text == null ? string.Empty : text.Trim();
            switch (type)
            {
                case FactValueType.Bool:
                    if (bool.TryParse(text, out var b)) { value = FactValue.FromBool(b); return true; }
                    break;
                case FactValueType.Int:
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        value = FactValue.FromInt(i);
                        return true;
                    }
                    break;
                case FactValueType.Float:
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        value = FactValue.FromFloat(f);
                        return true;
                    }
                    break;
                case FactValueType.String:
                    value = FactValue.FromString(text);
                    return true;
            }

            value = FactValue.DefaultFor(type);
            return false;
        }
    }
}
