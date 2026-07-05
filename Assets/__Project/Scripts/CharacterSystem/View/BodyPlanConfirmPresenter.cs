using System;
using CharacterSystem.Runtime;
using Core.Logging;
using Zenject;

namespace CharacterSystem.View
{
    /// <summary>
    /// Drives the body-plan confirm modal and implements the coordinator's prompt port:
    /// one request shows the dialog, and exactly one decision callback fires per request
    /// (confirm or decline both hide the dialog). Pure C#; never a MonoBehaviour.
    /// </summary>
    public class BodyPlanConfirmPresenter : IBodyPlanConfirmPrompt, IInitializable, IDisposable
    {
        private readonly IBodyPlanConfirmView _view;
        private readonly IGameLogger _logger;

        private Action<bool> _pendingDecision;

        public BodyPlanConfirmPresenter(IBodyPlanConfirmView view, IGameLogger logger)
        {
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            _view.OnConfirmed += HandleConfirmed;
            _view.OnDeclined += HandleDeclined;
            _view.Hide();
        }

        public void Dispose()
        {
            _view.OnConfirmed -= HandleConfirmed;
            _view.OnDeclined -= HandleDeclined;
        }

        public void Request(BodyPlanChangeSummary summary, Action<bool> decision)
        {
            if (decision == null)
            {
                throw new ArgumentNullException(nameof(decision));
            }

            if (_pendingDecision != null)
            {
                // The coordinator serializes transactions, so this is a programming error;
                // refuse the second request rather than orphaning the first callback.
                _logger.Error(LogCategory.CharacterSystem,
                    "[BodyPlanConfirm] A confirm request is already pending; rejecting the new one.");
                decision(false);
                return;
            }

            _pendingDecision = decision;
            _view.Show(summary);
        }

        private void HandleConfirmed()
        {
            Resolve(true);
        }

        private void HandleDeclined()
        {
            Resolve(false);
        }

        private void Resolve(bool confirmed)
        {
            if (_pendingDecision == null)
            {
                return;
            }

            var decision = _pendingDecision;
            _pendingDecision = null;
            _view.Hide();
            decision(confirmed);
        }
    }
}
