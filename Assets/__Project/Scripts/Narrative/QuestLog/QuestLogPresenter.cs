using System;
using Narrative.QuestLog.Core;
using Narrative.QuestLog.View;
using Narrative.Quests.Core;
using Narrative.Threads.Core;
using Zenject;

namespace Narrative.QuestLog
{
    /// <summary>
    /// MVP presenter for the quest log (P1-11): on every toggle it re-projects the CURRENT registry
    /// + thread ledger into a fresh read-only model (no cached staleness, no mutation) and flips the
    /// panel. Opening/closing has no gameplay effect by construction.
    /// </summary>
    public sealed class QuestLogPresenter : IInitializable, IDisposable
    {
        private readonly IQuestLogView _view;
        private readonly ILiveQuestRegistry _quests;
        private readonly IThreadLedger _threads;

        public QuestLogPresenter(IQuestLogView view, ILiveQuestRegistry quests, IThreadLedger threads)
        {
            _view = view;
            _quests = quests;
            _threads = threads;
        }

        public void Initialize()
        {
            _view.OnToggleRequested += HandleToggle;
        }

        public void Dispose()
        {
            _view.OnToggleRequested -= HandleToggle;
        }

        private void HandleToggle()
        {
            if (_view.IsVisible)
            {
                _view.Hide();
                return;
            }

            _view.Show(QuestLogModelBuilder.Build(_quests, _threads));
        }
    }
}
