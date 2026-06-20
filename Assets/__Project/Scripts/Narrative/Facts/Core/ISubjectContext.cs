namespace Narrative.Facts.Core
{
    /// <summary>
    /// Read-only bag of context bindings a casting supplies for subject-token resolution: e.g.
    /// <c>$self</c> → actor instance id, <c>$faction</c> → faction id, <c>$location</c> → location id.
    /// Keyed by the full token (including the leading <c>$</c>). The concrete bag is the casting's
    /// <c>ContextBag</c>; tests use a lightweight implementation.
    /// </summary>
    public interface ISubjectContext
    {
        bool TryGet(string token, out string subject);
    }
}
