using System.Collections.Generic;
using Combat.Player;

namespace Combat.View
{
    /// <summary>
    /// The turn-order strip's view contract (D2): a thin adapter the pure
    /// <see cref="TurnOrderStripPresenter"/> drives. It only renders the ordered actor list;
    /// all ordering / current-actor logic lives in the presenter.
    /// </summary>
    public interface ITurnOrderStripView
    {
        /// <summary>Renders the round's actors in order (leader first); replaces any prior contents.</summary>
        void SetEntries(IReadOnlyList<TurnOrderEntryModel> entries);

        /// <summary>Clears the strip (combat ended / disposed).</summary>
        void Clear();
    }
}
