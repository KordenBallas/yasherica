using System;
using CharacterSystem.Runtime;

namespace CharacterSystem.View
{
    /// <summary>
    /// Modal confirm dialog for a body-plan change that sheds parts: shows which frame the
    /// body will re-form into and which parts come off; the player confirms or declines.
    /// </summary>
    public interface IBodyPlanConfirmView
    {
        void Show(BodyPlanChangeSummary summary);

        void Hide();

        event Action OnConfirmed;

        event Action OnDeclined;
    }
}
