using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CharacterSystem.Data;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Combat.Arena.View;
using Core.Logging;
using UI.AbilityPreview;
using UnityEngine;
using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// The draft screen's presenter (G4): reads the <see cref="ArenaDraftFlow"/> replica and
    /// drives the uGUI view + the 3D stage; a legal local click becomes a transport pick
    /// REQUEST (the replica only ever advances on the host's broadcasts — req 15), an illegal
    /// one shows its reason (req 7). Also owns the cosmetic countdown (the authoritative
    /// deadline is the host's), the part-info popover content, the shared ability-preview
    /// routing, and the "your monster" beat with its auto-continue. Degrades gracefully when
    /// the panel prefab is missing: the beat auto-confirms so a draft never blocks the match.
    /// </summary>
    public class ArenaDraftPresenter : IInitializable, ITickable, IDisposable
    {
        private const string EmptySlotMark = "—";

        private readonly ArenaDraftFlow _flow;
        private readonly IArenaTransport _transport;
        private readonly IPartCatalog _partCatalog;
        private readonly ArenaDraftConfig _config;
        private readonly IArenaDraftClock _clock;
        private readonly IArenaDraftStage _stage;
        private readonly IAbilityPreviewPopover _abilityPopover;
        private readonly IGameLogger _logger;
        private readonly IArenaDraftView _view;
        private readonly IArenaPartInfoPopover _partPopover;

        private readonly Dictionary<string, string> _slotLabelsById =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private IReadOnlyList<ArenaPartAbilityViewData> _shownAbilities =
            Array.Empty<ArenaPartAbilityViewData>();
        private int _pendingEntryId = -1;
        private float _cosmeticDeadline;
        private float _beatDeadline;
        private bool _beatShowing;

        public ArenaDraftPresenter(
            ArenaDraftFlow flow,
            IArenaTransport transport,
            IPartCatalog partCatalog,
            ArenaDraftConfig config,
            IArenaDraftClock clock,
            IArenaDraftStage stage,
            IAbilityPreviewPopover abilityPopover,
            IGameLogger logger,
            [InjectOptional] IArenaDraftView view,
            [InjectOptional] IArenaPartInfoPopover partPopover)
        {
            _flow = flow;
            _transport = transport;
            _partCatalog = partCatalog;
            _config = config;
            _clock = clock;
            _stage = stage;
            _abilityPopover = abilityPopover;
            _logger = logger;
            _view = view;
            _partPopover = partPopover;
        }

        public void Initialize()
        {
            foreach (var slot in _config.SlotLoadout)
            {
                if (slot != null && !string.IsNullOrEmpty(slot.Id))
                {
                    _slotLabelsById[slot.Id] =
                        string.IsNullOrEmpty(slot.DisplayName) ? slot.Id : slot.DisplayName;
                }
            }

            _flow.DraftOpened += HandleDraftOpened;
            _flow.PickApplied += HandlePickApplied;
            _flow.DraftCompleted += HandleDraftCompleted;

            if (_view != null)
            {
                _view.BoardClicked += HandleBoardClicked;
                _view.BeatConfirmed += ConfirmBeat;
            }
            else
            {
                _logger.Warning(LogCategory.Combat,
                    "[ArenaDraftPresenter] No draft view bound (panel prefab missing?). The draft " +
                    "runs headless: local picks rely on the host's soft-timer auto-pick.");
            }

            if (_partPopover != null)
            {
                _partPopover.AbilityHovered += HandleAbilityHovered;
                _partPopover.DraftClicked += HandleDraftClicked;
            }
        }

        public void Dispose()
        {
            _flow.DraftOpened -= HandleDraftOpened;
            _flow.PickApplied -= HandlePickApplied;
            _flow.DraftCompleted -= HandleDraftCompleted;

            if (_view != null)
            {
                _view.BoardClicked -= HandleBoardClicked;
                _view.BeatConfirmed -= ConfirmBeat;
            }

            if (_partPopover != null)
            {
                _partPopover.AbilityHovered -= HandleAbilityHovered;
                _partPopover.DraftClicked -= HandleDraftClicked;
            }
        }

        public void Tick()
        {
            if (_beatShowing && _clock.Now >= _beatDeadline)
            {
                ConfirmBeat();
                return;
            }

            if (_view != null && _flow.Model != null && !_flow.Model.IsComplete)
            {
                _view.SetTimer(_cosmeticDeadline - _clock.Now, visible: true);
            }
        }

        // ---- flow → screen ----

        private void HandleDraftOpened()
        {
            var model = _flow.Model;
            _stage.Build(model.Board, model.SlotLoadout);
            _stage.UpdateLocalHero(model.LoadoutOf(_flow.LocalPlayerId));
            ArmCosmeticTimer();

            if (_view == null)
            {
                return;
            }

            _view.SetBoardTexture(_stage.Texture);
            RefreshTexts();
            _view.ShowPanel();
        }

        private void HandlePickApplied(ArenaDraftPick pick)
        {
            HidePopovers();

            if (pick.PlayerId != _flow.LocalPlayerId && _view != null
                && _stage.TryGetEntryViewportPoint(pick.EntryId, out var uv))
            {
                _view.PlayRemotePickFlight(uv, IconOfEntry(pick.EntryId));
            }

            _stage.RemoveEntry(pick.EntryId);
            if (pick.PlayerId == _flow.LocalPlayerId)
            {
                _stage.UpdateLocalHero(_flow.Model.LoadoutOf(_flow.LocalPlayerId));
            }

            ArmCosmeticTimer();
            RefreshTexts();
        }

        private void HandleDraftCompleted()
        {
            HidePopovers();

            if (_view == null)
            {
                // Headless degrade: never hold the match on a missing panel.
                _flow.ConfirmCompletion();
                return;
            }

            _beatShowing = true;
            _beatDeadline = _clock.Now + _config.BeatSeconds;
            _view.SetTimer(0f, visible: false);
            _view.ShowBeat(
                $"{_flow.PlayerNameOf(_flow.LocalPlayerId)} — your monster",
                LoadoutText(_flow.LocalPlayerId));
        }

        private void ConfirmBeat()
        {
            if (!_beatShowing && _view != null)
            {
                return;
            }

            _beatShowing = false;
            if (_view != null)
            {
                _view.HideBeat();
                _view.HidePanel();
            }

            _stage.Clear();
            _flow.ConfirmCompletion();
        }

        // ---- clicks & popovers ----

        private void HandleBoardClicked(Vector2 uv)
        {
            if (_partPopover == null || _flow.Model == null || !_stage.TryRaycast(uv, out var hit))
            {
                HidePopovers();
                return;
            }

            var screenPosition = _view.BoardUvToScreen(uv);
            if (hit.IsBoardEntry)
            {
                ShowEntryInfo(hit.EntryId, screenPosition);
            }
            else if (hit.IsHeroPart)
            {
                ShowHeroPartInfo(hit.HeroSlotId, screenPosition);
            }
        }

        private void ShowEntryInfo(int entryId, Vector2 screenPosition)
        {
            var model = _flow.Model;
            var entry = model.Board.FirstOrDefault(e => e.EntryId == entryId);
            if (entry == null || !_partCatalog.TryGet(entry.PartId, out var part))
            {
                return;
            }

            string note;
            bool canDraft = EvaluatePickLegality(entry, out note);
            _pendingEntryId = entryId;
            _shownAbilities = BuildAbilityRows(part);
            _partPopover.Show(new ArenaPartInfoViewData(
                part.DisplayName, SlotLabel(entry.SlotId), _shownAbilities, canDraft, note),
                screenPosition);
        }

        private void ShowHeroPartInfo(string slotId, Vector2 screenPosition)
        {
            var loadout = _flow.Model.LoadoutOf(_flow.LocalPlayerId);
            if (!loadout.TryGetValue(slotId, out var partId)
                || !_partCatalog.TryGet(partId, out var part))
            {
                return;
            }

            _pendingEntryId = -1;
            _shownAbilities = BuildAbilityRows(part);
            _partPopover.Show(new ArenaPartInfoViewData(
                part.DisplayName, SlotLabel(slotId), _shownAbilities, canDraft: false, note: "Drafted"),
                screenPosition);
        }

        private void HandleDraftClicked()
        {
            var model = _flow.Model;
            if (model == null || _pendingEntryId < 0)
            {
                return;
            }

            _transport.SubmitDraftPick(new ArenaDraftPick(
                model.PickIndex, _flow.LocalPlayerId, _pendingEntryId, wasAutoPick: false));
            HidePopovers();
        }

        private void HandleAbilityHovered(int index, Vector2 screenPosition, bool entering)
        {
            if (!entering)
            {
                _abilityPopover.Hide();
                return;
            }

            if (index >= 0 && index < _shownAbilities.Count)
            {
                _abilityPopover.Show(_shownAbilities[index].Preview, screenPosition);
            }
        }

        private void HidePopovers()
        {
            _pendingEntryId = -1;
            _partPopover?.Hide();
            _abilityPopover.Hide();
        }

        // ---- content building ----

        private bool EvaluatePickLegality(ArenaDraftBoardEntry entry, out string note)
        {
            var model = _flow.Model;
            if (model.IsComplete)
            {
                note = "The draft is over.";
                return false;
            }

            if (model.CurrentPickerPlayerId != _flow.LocalPlayerId)
            {
                note = $"Waiting for {_flow.PlayerNameOf(model.CurrentPickerPlayerId)}.";
                return false;
            }

            if (!model.IsEntryAvailable(entry.EntryId))
            {
                note = "Already taken.";
                return false;
            }

            if (model.LoadoutOf(_flow.LocalPlayerId).ContainsKey(entry.SlotId))
            {
                note = $"Your {SlotLabel(entry.SlotId)} slot is already filled.";
                return false;
            }

            note = string.Empty;
            return true;
        }

        private IReadOnlyList<ArenaPartAbilityViewData> BuildAbilityRows(
            CharacterSystem.Data.Definitions.PartDefinition part)
        {
            var rows = new List<ArenaPartAbilityViewData>();
            foreach (var ability in part.ActiveAbilities)
            {
                if (ability != null)
                {
                    rows.Add(new ArenaPartAbilityViewData(
                        ability.Name, ability.Icon, AbilityPreviewData.FromAbility(ability)));
                }
            }

            foreach (var passive in part.PassiveAbilities)
            {
                if (passive != null)
                {
                    rows.Add(new ArenaPartAbilityViewData(
                        $"{passive.Name} (passive)", passive.Icon, AbilityPreviewData.FromPassive(passive)));
                }
            }

            return rows;
        }

        private void RefreshTexts()
        {
            if (_view == null || _flow.Model == null)
            {
                return;
            }

            var model = _flow.Model;
            if (!model.IsComplete)
            {
                int picker = model.CurrentPickerPlayerId;
                _view.SetTurn(_flow.PlayerNameOf(picker), picker == _flow.LocalPlayerId);
            }

            _view.SetOrderLine(OrderLine(model));
            _view.SetLocalReadout(
                $"<b>{_flow.PlayerNameOf(_flow.LocalPlayerId)}</b> (you)\n{LoadoutText(_flow.LocalPlayerId)}");
            _view.SetOpponentsReadout(OpponentsText(model));
        }

        private string OrderLine(ArenaDraftModel model)
        {
            var seats = model.SeatOrder;
            bool reversed = (model.PickIndex / seats.Count) % 2 == 1;
            var names = seats.Select(id =>
                !model.IsComplete && id == model.CurrentPickerPlayerId
                    ? $"<color=#FFD84D><b>{_flow.PlayerNameOf(id)}</b></color>"
                    : _flow.PlayerNameOf(id));
            return string.Join("  →  ", reversed ? names.Reverse() : names) + "   (snake draft)";
        }

        private string OpponentsText(ArenaDraftModel model)
        {
            var builder = new StringBuilder();
            foreach (var playerId in model.SeatOrder)
            {
                if (playerId == _flow.LocalPlayerId)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine($"<b>{_flow.PlayerNameOf(playerId)}</b>");
                builder.Append(LoadoutText(playerId));
            }

            return builder.ToString();
        }

        private string LoadoutText(int playerId)
        {
            var loadout = _flow.Model.LoadoutOf(playerId);
            var builder = new StringBuilder();
            foreach (var slotId in _flow.Model.SlotLoadout)
            {
                string partName = EmptySlotMark;
                if (loadout.TryGetValue(slotId, out var partId))
                {
                    partName = _partCatalog.TryGet(partId, out var part) ? part.DisplayName : partId;
                }

                builder.AppendLine($"  {SlotLabel(slotId)}: {partName}");
            }

            return builder.ToString().TrimEnd();
        }

        private string SlotLabel(string slotId) =>
            _slotLabelsById.TryGetValue(slotId, out var label) ? label : slotId;

        private Sprite IconOfEntry(int entryId)
        {
            var entry = _flow.Model?.Board.FirstOrDefault(e => e.EntryId == entryId);
            return entry != null && _partCatalog.TryGet(entry.PartId, out var part)
                ? part.ChoiceIcon
                : null;
        }

        private void ArmCosmeticTimer()
        {
            _cosmeticDeadline = _clock.Now + _flow.PickTimerSeconds;
        }
    }
}
