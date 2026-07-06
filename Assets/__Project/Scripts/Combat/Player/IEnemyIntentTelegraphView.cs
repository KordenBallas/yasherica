using System.Collections.Generic;

namespace Combat.Player
{
    /// <summary>
    /// View contract for the enemy committed-intent board telegraph (D3): the move-direction arrow and
    /// the "armed / about to act" body pose. Thin adapter driven by
    /// <see cref="EnemyIntentTelegraphPresenter"/>; the presenter owns all the logic.
    /// </summary>
    public interface IEnemyIntentTelegraphView
    {
        /// <summary>Renders the current armed enemies (arrows + poses); enemies absent from the list are cleared.</summary>
        void SetTelegraphs(IReadOnlyList<EnemyIntentTelegraphModel> telegraphs);

        /// <summary>Clears every arrow and restores every posed enemy (combat ended / disposed).</summary>
        void Clear();
    }
}
