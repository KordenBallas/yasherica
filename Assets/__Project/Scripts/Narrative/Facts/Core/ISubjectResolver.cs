namespace Narrative.Facts.Core
{
    /// <summary>
    /// Turns an unresolved subject token into the concrete subject id used to build a
    /// <see cref="FactKey"/>. Built-in tokens (<c>$self</c>/<c>$target</c>/<c>$faction</c>) and any
    /// arbitrary <c>$&lt;contextKey&gt;</c> (e.g. <c>$location</c>) resolve via the casting context
    /// (A1); empty token = global; a literal (non-<c>$</c>) token passes through as a concrete id.
    /// An unknown <c>$</c>-token fails closed (returns false) so authoring errors surface.
    /// </summary>
    public interface ISubjectResolver
    {
        bool TryResolve(string token, ISubjectContext context, out string subject);
    }
}
