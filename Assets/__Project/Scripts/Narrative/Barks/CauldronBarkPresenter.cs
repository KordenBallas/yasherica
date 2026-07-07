using System;
using Narrative.Barks.Core;
using Narrative.Barks.View;
using Zenject;

namespace Narrative.Barks
{
    /// <summary>
    /// MVP presenter for the bark channel (P1-10): relays every fired bark line to the bubble view.
    /// Holds no state — the service owns selection, the view owns timing.
    /// </summary>
    public sealed class CauldronBarkPresenter : IInitializable, IDisposable
    {
        private readonly ICauldronBarkService _service;
        private readonly ICauldronBarkView _view;

        public CauldronBarkPresenter(ICauldronBarkService service, ICauldronBarkView view)
        {
            _service = service;
            _view = view;
        }

        public void Initialize()
        {
            _service.OnBark += HandleBark;
        }

        public void Dispose()
        {
            _service.OnBark -= HandleBark;
        }

        private void HandleBark(string line) => _view.ShowBark(line);
    }
}
