using System;
using System.Collections.Generic;

namespace CharacterSystem.View
{
    /// <summary>
    /// Dev console for the body-plan demo scene: one dropdown per slot listing every
    /// authored part. Rebuilt wholesale on every body change (dev tooling — simplicity
    /// over churn); picking an option raises <see cref="OnPartPicked"/>.
    /// </summary>
    public interface IBodyPlanDemoConsoleView
    {
        void ShowRows(IReadOnlyList<BodyPlanDemoRowViewData> rows);

        /// <summary>Raised with (slotId, partId) when the player picks a part option.</summary>
        event Action<string, string> OnPartPicked;
    }
}
