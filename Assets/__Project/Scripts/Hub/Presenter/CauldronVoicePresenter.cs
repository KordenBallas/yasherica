using System;
using Core.Persistence;
using Hub.Core;
using Hub.Data;
using Hub.View;
using Zenject;

namespace Hub.Presenter
{
    /// <summary>
    /// Speaks the cauldron's Hub lines (O1): the death-return greeting (consumes the one-shot
    /// arrival marker), the empty-offer remark, and reactions to the part pick and the launch.
    /// Lines are data-authored (<c>HubVoiceLinesConfig</c>) and picked deterministically; an empty
    /// pool is a quiet cauldron, never an error. Must initialize AFTER the staging presenter (the
    /// empty-offer check reads the dealt offer) — the installer orders this explicitly.
    /// </summary>
    public sealed class CauldronVoicePresenter : IInitializable, IDisposable
    {
        private readonly HubStagingModel _model;
        private readonly IHubVoiceView _view;
        private readonly CauldronVoiceLines _lines;
        private readonly IHubArrivalStore _arrival;
        private readonly HubMetaReader _meta;

        private int _salt;

        public CauldronVoicePresenter(HubStagingModel model, IHubVoiceView view,
            CauldronVoiceLines lines, IHubArrivalStore arrival, HubMetaReader meta)
        {
            _model = model;
            _view = view;
            _lines = lines;
            _arrival = arrival;
            _meta = meta;
        }

        public void Initialize()
        {
            _salt = _meta.ReadRunCount() + 1;
            _model.PartChosen += HandlePartChosen;
            _model.Launching += HandleLaunching;

            if (_arrival.TryConsumeDeathReturn())
            {
                Speak(CauldronVoiceMoment.DeathReturn, null);
            }
            else if (_model.Offer.Count == 0)
            {
                Speak(CauldronVoiceMoment.NoPartAvailable, null);
            }
        }

        public void Dispose()
        {
            _model.PartChosen -= HandlePartChosen;
            _model.Launching -= HandleLaunching;
        }

        private void HandlePartChosen(StartingPartCandidate candidate)
        {
            Speak(CauldronVoiceMoment.PartPicked, candidate.RaceId);
        }

        private void HandleLaunching()
        {
            Speak(CauldronVoiceMoment.Launch, null);
        }

        private void Speak(CauldronVoiceMoment moment, string raceId)
        {
            if (CauldronVoiceSelector.TrySelect(_lines, moment, raceId, _salt, out var line))
            {
                _view.ShowLine(line);
            }
        }
    }
}
