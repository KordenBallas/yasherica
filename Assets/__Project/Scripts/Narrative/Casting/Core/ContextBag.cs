using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// The per-casting context (R3): subject-token bindings used for fact resolution (e.g.
    /// <c>$self</c> → actor instance id, <c>$faction</c> → faction id, <c>$location</c> → location id)
    /// plus the Ink variables the dialogue layer injects on fresh start (names, items, availability
    /// flags). Implements <see cref="ISubjectContext"/> over its subject bindings.
    /// </summary>
    public sealed class ContextBag : ISubjectContext
    {
        private readonly Dictionary<string, string> _subjects = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();

        /// <summary>Binds a subject token (including the leading <c>$</c>) to a concrete id.</summary>
        public ContextBag BindSubject(string token, string subject)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _subjects[token] = subject ?? string.Empty;
            }

            return this;
        }

        /// <summary>Sets an Ink variable value injected on fresh start (W2-2/W2-4).</summary>
        public ContextBag SetVariable(string name, object value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _variables[name] = value;
            }

            return this;
        }

        public IReadOnlyDictionary<string, object> Variables => _variables;

        public bool TryGetVariable(string name, out object value) => _variables.TryGetValue(name ?? string.Empty, out value);

        public bool TryGet(string token, out string subject) => _subjects.TryGetValue(token ?? string.Empty, out subject);
    }
}
