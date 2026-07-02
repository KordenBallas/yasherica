using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Data;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Mutation.Presenter
{
    /// <summary>
    /// Drives the operating-table rack: seeds the starting blanks (dev seed until
    /// blanks drop as loot), renders the racked blanks with their socket state, and
    /// routes the view's drop/unsocket gestures into the socketing model. Holds no
    /// domain state - the rack and socketing model are the truth; every change
    /// event triggers a full (small-N) rebuild.
    /// </summary>
    public class BlankRackPresenter : IInitializable, IDisposable
    {
        private readonly IBlankRack _rack;
        private readonly ISocketingModel _socketing;
        private readonly IPartBlankCatalog _blankCatalog;
        private readonly IArchetypeCatalog _archetypeCatalog;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly MutationConfig _config;
        private readonly IBlankRackView _view;
        private readonly IGameLogger _logger;

        public BlankRackPresenter(
            IBlankRack rack,
            ISocketingModel socketing,
            IPartBlankCatalog blankCatalog,
            IArchetypeCatalog archetypeCatalog,
            IArtifactCatalog artifactCatalog,
            MutationConfig config,
            IBlankRackView view,
            IGameLogger logger)
        {
            _rack = rack;
            _socketing = socketing;
            _blankCatalog = blankCatalog;
            _archetypeCatalog = archetypeCatalog;
            _artifactCatalog = artifactCatalog;
            _config = config;
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            SeedStartingBlanks();

            _rack.OnChanged += Refresh;
            _socketing.OnSocketsChanged += HandleSocketsChanged;
            _view.OnArtifactDroppedOnBlank += HandleArtifactDropped;
            _view.OnFilledSocketClicked += HandleFilledSocketClicked;

            Refresh();
        }

        public void Dispose()
        {
            _rack.OnChanged -= Refresh;
            _socketing.OnSocketsChanged -= HandleSocketsChanged;
            _view.OnArtifactDroppedOnBlank -= HandleArtifactDropped;
            _view.OnFilledSocketClicked -= HandleFilledSocketClicked;
        }

        private void SeedStartingBlanks()
        {
            // Dev seed only while the rack is untouched, so a later re-initialize
            // never duplicates blanks.
            if (_rack.Blanks.Count > 0)
            {
                return;
            }

            foreach (var definition in _config.StartingBlanks)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                if (!_rack.TryAdd(definition.Id, out _))
                {
                    _logger.Warning(LogCategory.Mutation,
                        $"[BlankRackPresenter] Starting blank '{definition.Id}' did not fit the rack " +
                        $"(capacity {_rack.Capacity}).");
                }
            }
        }

        private void HandleSocketsChanged(int _)
        {
            Refresh();
        }

        private void HandleArtifactDropped(int artifactInstanceId, int blankInstanceId)
        {
            if (!_socketing.TrySocket(blankInstanceId, artifactInstanceId))
            {
                _logger.Info(LogCategory.Mutation,
                    $"[BlankRackPresenter] Socketing artifact {artifactInstanceId} into blank " +
                    $"{blankInstanceId} rejected (full blank or the item is not in the inventory).");
            }
        }

        private void HandleFilledSocketClicked(int blankInstanceId, int artifactInstanceId)
        {
            if (!_socketing.TryUnsocket(blankInstanceId, artifactInstanceId))
            {
                // A full blank is committed - the last drop was the point of no return.
                _logger.Info(LogCategory.Mutation,
                    $"[BlankRackPresenter] Unsocketing artifact {artifactInstanceId} from blank " +
                    $"{blankInstanceId} rejected (committed or not socketed).");
            }
        }

        private void Refresh()
        {
            var blanks = _rack.Blanks;
            var entries = new List<BlankEntryViewData>(blanks.Count);
            foreach (var instance in blanks)
            {
                if (!_blankCatalog.TryGet(instance.DefinitionId, out var blank))
                {
                    _logger.Warning(LogCategory.Mutation,
                        $"[BlankRackPresenter] No blank definition for id '{instance.DefinitionId}'.");
                    continue;
                }

                var tint = Color.white;
                if (_archetypeCatalog.TryGet(blank.SpeciesArchetypeId, out var archetype))
                {
                    tint = archetype.Tint;
                }

                _blankCatalog.TryGetIcon(instance.DefinitionId, out var icon);

                entries.Add(new BlankEntryViewData(
                    instance.InstanceId,
                    blank.DisplayName,
                    icon,
                    tint,
                    blank.SocketCount,
                    BuildFilledSockets(instance.InstanceId)));
            }

            _view.ShowBlanks(entries);
        }

        private IReadOnlyList<SocketViewData> BuildFilledSockets(int blankInstanceId)
        {
            var socketed = _socketing.SocketedArtifacts(blankInstanceId);
            var filled = new List<SocketViewData>(socketed.Count);
            foreach (var artifact in socketed)
            {
                Sprite icon = null;
                var tint = Color.white;
                if (_artifactCatalog.TryGet(artifact.DefinitionId, out var definition))
                {
                    icon = definition.Icon;
                    tint = definition.BubbleTint;
                }

                filled.Add(new SocketViewData(artifact.InstanceId, icon, tint));
            }

            return filled;
        }
    }
}
