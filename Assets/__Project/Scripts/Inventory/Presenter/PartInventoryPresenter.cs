using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using Inventory.Core;
using Inventory.View;
using Zenject;

namespace Inventory.Presenter
{
    /// <summary>
    /// Presents the stored-parts stash: on every addition, aggregates the multiset into
    /// name×count rows (friendly names resolved through the part catalog — the same
    /// pragmatic cross-system read the mutation catalog uses) and pushes them to the view.
    /// Pure C#; never a MonoBehaviour.
    /// </summary>
    public class PartInventoryPresenter : IInitializable, IDisposable
    {
        private readonly IPartInventoryModel _model;
        private readonly IPartInventoryView _view;
        private readonly IPartCatalog _partCatalog;

        public PartInventoryPresenter(
            IPartInventoryModel model,
            IPartInventoryView view,
            IPartCatalog partCatalog)
        {
            _model = model;
            _view = view;
            _partCatalog = partCatalog;
        }

        public void Initialize()
        {
            _model.OnPartAdded += HandlePartAdded;
            Refresh();
        }

        public void Dispose()
        {
            _model.OnPartAdded -= HandlePartAdded;
        }

        private void HandlePartAdded(string partId)
        {
            Refresh();
        }

        private void Refresh()
        {
            var countsById = new Dictionary<string, int>(StringComparer.Ordinal);
            var orderedIds = new List<string>();
            foreach (var partId in _model.PartIds)
            {
                if (countsById.TryGetValue(partId, out var count))
                {
                    countsById[partId] = count + 1;
                }
                else
                {
                    countsById[partId] = 1;
                    orderedIds.Add(partId);
                }
            }

            var items = new List<PartInventoryItemViewData>(orderedIds.Count);
            foreach (var partId in orderedIds)
            {
                items.Add(new PartInventoryItemViewData(ResolveDisplayName(partId), countsById[partId]));
            }

            _view.ShowParts(items);
        }

        private string ResolveDisplayName(string partId)
        {
            if (_partCatalog.TryGet(partId, out var part))
            {
                return string.IsNullOrEmpty(part.DisplayName) ? part.name : part.DisplayName;
            }

            return partId;
        }
    }
}
