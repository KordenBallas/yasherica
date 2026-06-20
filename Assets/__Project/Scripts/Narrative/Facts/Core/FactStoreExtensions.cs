namespace Narrative.Facts.Core
{
    /// <summary>
    /// Type-safe read/write helpers over <see cref="IFactStore"/> driven by a <see cref="FactKeyRef"/>.
    /// Reads use the value type's natural default as the unset fallback; the store still enforces the
    /// registry on writes. Keeps call sites free of manual <see cref="FactKey"/> + <see cref="FactValue"/>
    /// plumbing.
    /// </summary>
    public static class FactStoreExtensions
    {
        public static bool GetBool(this IFactStore store, FactKeyRef key, string subject = "")
            => store.GetOrDefault(key.ToKey(subject), FactValue.FromBool(false)).AsBool();

        public static void SetBool(this IFactStore store, FactKeyRef key, bool value, string subject = "")
            => store.Set(key.ToKey(subject), FactValue.FromBool(value));

        public static long GetInt(this IFactStore store, FactKeyRef key, string subject = "")
            => store.GetOrDefault(key.ToKey(subject), FactValue.FromInt(0)).AsInt();

        public static void SetInt(this IFactStore store, FactKeyRef key, long value, string subject = "")
            => store.Set(key.ToKey(subject), FactValue.FromInt(value));

        public static double GetFloat(this IFactStore store, FactKeyRef key, string subject = "")
            => store.GetOrDefault(key.ToKey(subject), FactValue.FromFloat(0d)).AsFloat();

        public static void SetFloat(this IFactStore store, FactKeyRef key, double value, string subject = "")
            => store.Set(key.ToKey(subject), FactValue.FromFloat(value));

        public static string GetString(this IFactStore store, FactKeyRef key, string subject = "")
            => store.GetOrDefault(key.ToKey(subject), FactValue.FromString(string.Empty)).AsString();

        public static void SetString(this IFactStore store, FactKeyRef key, string value, string subject = "")
            => store.Set(key.ToKey(subject), FactValue.FromString(value));
    }
}
