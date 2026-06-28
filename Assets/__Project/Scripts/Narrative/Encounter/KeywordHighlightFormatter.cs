using System.Text;

namespace Narrative.Encounter
{
    /// <summary>
    /// Turns author-marked key words into highlighted rich text (Encounter UI R11/R12). Writers wrap a
    /// key word in double brackets — <c>[[Garrick]]</c> — and this converts each mark into a TMP
    /// <c>&lt;color=#hex&gt;</c> span while the brackets themselves are never shown. Unmarked text passes
    /// through untouched; an unterminated <c>[[</c> is emitted verbatim (fails safe, never throws).
    ///
    /// Pure string→string and UnityEngine-free so it can live beside the presenter and be unit-tested;
    /// both the situation line and the card text run through it so a key word reads the same everywhere.
    /// </summary>
    public static class KeywordHighlightFormatter
    {
        private const string OpenMarker = "[[";
        private const string CloseMarker = "]]";

        /// <summary>
        /// Replaces every <c>[[word]]</c> with the word wrapped in a <c>&lt;color=#hex&gt;</c> span and
        /// removes the markers. When <paramref name="hexColor"/> is null/empty the markers are still
        /// stripped but no color is applied (the word renders normally). An empty mark <c>[[]]</c> yields
        /// nothing. A leading <c>#</c> on <paramref name="hexColor"/> is accepted.
        /// </summary>
        public static string ToRichText(string raw, string hexColor)
        {
            if (string.IsNullOrEmpty(raw) || raw.IndexOf(OpenMarker, System.StringComparison.Ordinal) < 0)
            {
                return raw;
            }

            var hex = NormalizeHex(hexColor);
            var sb = new StringBuilder(raw.Length + 16);
            int i = 0;
            while (i < raw.Length)
            {
                int open = raw.IndexOf(OpenMarker, i, System.StringComparison.Ordinal);
                if (open < 0)
                {
                    sb.Append(raw, i, raw.Length - i);
                    break;
                }

                int contentStart = open + OpenMarker.Length;
                int close = raw.IndexOf(CloseMarker, contentStart, System.StringComparison.Ordinal);
                if (close < 0)
                {
                    // Unterminated mark: emit the rest verbatim so nothing is silently dropped.
                    sb.Append(raw, i, raw.Length - i);
                    break;
                }

                sb.Append(raw, i, open - i); // text before the mark
                AppendHighlighted(sb, raw.Substring(contentStart, close - contentStart), hex);
                i = close + CloseMarker.Length;
            }

            return sb.ToString();
        }

        private static void AppendHighlighted(StringBuilder sb, string word, string hex)
        {
            if (string.IsNullOrEmpty(word))
            {
                return; // [[]] marks nothing
            }

            if (string.IsNullOrEmpty(hex))
            {
                sb.Append(word);
                return;
            }

            sb.Append("<color=#").Append(hex).Append('>').Append(word).Append("</color>");
        }

        private static string NormalizeHex(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
            {
                return string.Empty;
            }

            return hexColor[0] == '#' ? hexColor.Substring(1) : hexColor;
        }
    }
}
