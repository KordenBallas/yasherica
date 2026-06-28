using System.Collections.Generic;

namespace DevTools.Core
{
    /// <summary>
    /// One titled block of read-only display rows for the developer overlay. A pure-C# view-model the
    /// <see cref="IDevStateSource"/> builds and a thin view renders; carries no behaviour.
    /// </summary>
    public sealed class DevPanelSection
    {
        public string Title { get; }
        public IReadOnlyList<string> Rows { get; }

        public DevPanelSection(string title, IReadOnlyList<string> rows)
        {
            Title = title ?? string.Empty;
            Rows = rows ?? System.Array.Empty<string>();
        }
    }
}
