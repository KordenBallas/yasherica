using Core.Logging;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Default <see cref="ISubjectResolver"/>. Resolution rules (A1):
    /// <list type="bullet">
    /// <item>empty/null token → global subject (empty string)</item>
    /// <item><c>$&lt;contextKey&gt;</c> → looked up in the casting context (incl. built-ins
    /// <c>$self</c>/<c>$target</c>/<c>$faction</c>/<c>$location</c>); missing → fail closed + warn</item>
    /// <item>any other literal → passed through as a concrete subject id</item>
    /// </list>
    /// Pure C#.
    /// </summary>
    public sealed class SubjectResolver : ISubjectResolver
    {
        private const char TokenPrefix = '$';
        private readonly IGameLogger _logger;

        public SubjectResolver(IGameLogger logger = null)
        {
            _logger = logger;
        }

        public bool TryResolve(string token, ISubjectContext context, out string subject)
        {
            if (string.IsNullOrEmpty(token))
            {
                subject = string.Empty;
                return true;
            }

            if (token[0] != TokenPrefix)
            {
                // A literal concrete subject id authored directly.
                subject = token;
                return true;
            }

            if (context != null && context.TryGet(token, out subject) && !string.IsNullOrEmpty(subject))
            {
                return true;
            }

            _logger?.Warning(LogCategory.Narrative,$"[SubjectResolver] Unresolved subject token '{token}' - failing closed.");
            subject = string.Empty;
            return false;
        }
    }
}
