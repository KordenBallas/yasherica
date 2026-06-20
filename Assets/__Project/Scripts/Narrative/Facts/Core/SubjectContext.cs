using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Simple dictionary-backed <see cref="ISubjectContext"/>. Tokens are stored including the
    /// leading <c>$</c> (e.g. <c>"$self"</c>). The casting <c>ContextBag</c> builds on this.
    /// </summary>
    public class SubjectContext : ISubjectContext
    {
        private readonly Dictionary<string, string> _bindings = new Dictionary<string, string>();

        public SubjectContext Bind(string token, string subject)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _bindings[token] = subject ?? string.Empty;
            }

            return this;
        }

        public bool TryGet(string token, out string subject)
        {
            return _bindings.TryGetValue(token ?? string.Empty, out subject);
        }
    }
}
