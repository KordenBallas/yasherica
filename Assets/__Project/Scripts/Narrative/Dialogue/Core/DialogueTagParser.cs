using System;
using Core.Logging;
using Narrative.Facts.Core;

namespace Narrative.Dialogue.Core
{
    /// <summary>
    /// Parses Ink <c>key: value</c> tags into typed <see cref="DialogueTag"/>s — the only bridge from
    /// Ink to game systems (R4); Ink never references C# types. The fact grammar is
    /// <c>fact: &lt;ns&gt;.&lt;subject?&gt;.&lt;key&gt; &lt;op&gt; &lt;value&gt;</c>; the value is typed via the
    /// registry so authors write plain literals. Malformed tags fail closed (return Unknown) and warn.
    /// Pure C#.
    /// </summary>
    public sealed class DialogueTagParser
    {
        private readonly IFactKeyRegistry _registry;
        private readonly IGameLogger _logger;

        public DialogueTagParser(IFactKeyRegistry registry, IGameLogger logger = null)
        {
            _registry = registry;
            _logger = logger;
        }

        public DialogueTag Parse(string rawTag)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                return DialogueTag.Of(DialogueTagKind.Unknown, string.Empty);
            }

            int colon = rawTag.IndexOf(':');
            if (colon <= 0)
            {
                return DialogueTag.Of(DialogueTagKind.Unknown, rawTag.Trim());
            }

            var key = rawTag.Substring(0, colon).Trim().ToLowerInvariant();
            var arg = rawTag.Substring(colon + 1).Trim();

            switch (key)
            {
                case "speaker": return DialogueTag.Of(DialogueTagKind.Speaker, arg);
                case "offer-quest": return DialogueTag.Of(DialogueTagKind.OfferQuest, arg);
                case "advance-objective": return DialogueTag.Of(DialogueTagKind.AdvanceObjective, arg);
                case "complete-quest": return DialogueTag.Of(DialogueTagKind.CompleteQuest, arg);
                case "fail-quest": return DialogueTag.Of(DialogueTagKind.FailQuest, arg);
                case "start-combat": return DialogueTag.Of(DialogueTagKind.StartCombat, arg);
                case "outcome": return DialogueTag.Of(DialogueTagKind.Outcome, arg);
                case "fact": return ParseFact(arg);
                default: return DialogueTag.Of(DialogueTagKind.Unknown, rawTag.Trim());
            }
        }

        private DialogueTag ParseFact(string arg)
        {
            var parts = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                Warn($"malformed fact tag '{arg}' (expected '<path> <op> [value]')");
                return DialogueTag.Of(DialogueTagKind.Unknown, arg);
            }

            if (!TrySplitPath(parts[0], out var ns, out var subject, out var bareKey))
            {
                Warn($"malformed fact path '{parts[0]}'");
                return DialogueTag.Of(DialogueTagKind.Unknown, arg);
            }

            if (!TryParseOp(parts[1], out var op))
            {
                Warn($"unknown fact op '{parts[1]}'");
                return DialogueTag.Of(DialogueTagKind.Unknown, arg);
            }

            if (_registry == null || !_registry.TryGetInfo(ns, bareKey, out var info))
            {
                Warn($"unknown fact key '{ns}.{bareKey}' - cannot type value");
                return DialogueTag.Of(DialogueTagKind.Unknown, arg);
            }

            var valueText = parts.Length >= 3 ? string.Join(" ", parts, 2, parts.Length - 2) : string.Empty;
            FactValue value;
            if (op == FactEffectOp.Remove || (op == FactEffectOp.Toggle && parts.Length < 3))
            {
                value = FactValue.DefaultFor(info.ValueType);
            }
            else if (!FactValueConversion.TryParse(info.ValueType, valueText, out value))
            {
                Warn($"bad value '{valueText}' for {info.ValueType} key '{ns}.{bareKey}'");
                return DialogueTag.Of(DialogueTagKind.Unknown, arg);
            }

            return DialogueTag.OfFact(new FactEffectCore(ns, subject, bareKey, op, value));
        }

        private static bool TrySplitPath(string path, out FactNamespace ns, out string subject, out string key)
        {
            ns = FactNamespace.World;
            subject = string.Empty;
            key = string.Empty;

            var segments = path.Split('.');
            if (segments.Length < 2)
            {
                return false;
            }

            if (!Enum.TryParse(segments[0], true, out ns))
            {
                return false;
            }

            key = segments[segments.Length - 1];
            if (segments.Length >= 3)
            {
                // Everything between ns and key is the subject token (usually a single $token).
                subject = string.Join(".", segments, 1, segments.Length - 2);
            }

            return !string.IsNullOrEmpty(key);
        }

        private static bool TryParseOp(string text, out FactEffectOp op)
        {
            return Enum.TryParse(text, true, out op) && Enum.IsDefined(typeof(FactEffectOp), op);
        }

        private void Warn(string message) => _logger?.Warning($"[DialogueTagParser] {message} - ignored.");
    }
}
