namespace Narrative.Barks.View
{
    /// <summary>
    /// Adapter contract for the cauldron's in-run bark bubble (P1-10): one short line at a time,
    /// self-hiding after its display window. Never blocks input.
    /// </summary>
    public interface ICauldronBarkView
    {
        void ShowBark(string line);
    }
}
