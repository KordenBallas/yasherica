using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// State machine implementing the magic pot crafting rules:
    /// staging N items enters the Crafting state and waits for ResolveCraft (so the
    /// presentation can animate the merge first); resolving consumes the inputs and
    /// produces a detached result - every combine yields something (signature recipe
    /// or emergent fusion), there is no fail path. Unstaging during Crafting cancels
    /// the pending combine.
    /// </summary>
    public class CraftingSession : ICraftingSession
    {
        private const int MinimumItemsToCombine = 2;

        private readonly IFusionResolver _fusionResolver;
        private readonly IInventoryModel _inventory;
        private readonly int _itemsToCombine;
        private readonly List<ArtifactInstance> _stagedItems = new List<ArtifactInstance>();

        public CraftingState State { get; private set; } = CraftingState.Idle;
        public IReadOnlyList<ArtifactInstance> StagedItems => _stagedItems;
        public ArtifactInstance PendingResult { get; private set; }

        public event Action<ArtifactInstance> OnItemStaged;
        public event Action<IReadOnlyList<ArtifactInstance>> OnCraftingStarted;
        public event Action<ArtifactInstance, bool> OnCraftSucceeded;
        public event Action<ArtifactInstance> OnItemUnstaged;
        public event Action<ArtifactInstance> OnResultCollected;
        public event Action<IReadOnlyList<ArtifactInstance>> OnSessionCleared;

        public CraftingSession(IFusionResolver fusionResolver, IInventoryModel inventory, int itemsToCombine)
        {
            _fusionResolver = fusionResolver ?? throw new ArgumentNullException(nameof(fusionResolver));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            if (itemsToCombine < MinimumItemsToCombine)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemsToCombine),
                    $"Crafting requires at least {MinimumItemsToCombine} items to combine, got {itemsToCombine}.");
            }

            _itemsToCombine = itemsToCombine;
        }

        public bool TrySelect(int instanceId)
        {
            // The pending result must be collected, and a pending combine resolved
            // or cancelled, before new selections are accepted.
            if (State == CraftingState.ResultReady || State == CraftingState.Crafting)
            {
                return false;
            }

            if (!_inventory.TryGet(instanceId, out var instance))
            {
                return false;
            }

            _inventory.Remove(instanceId);
            _stagedItems.Add(instance);
            State = CraftingState.Selecting;
            OnItemStaged?.Invoke(instance);

            if (_stagedItems.Count >= _itemsToCombine)
            {
                // The combine is deferred to ResolveCraft so the presentation can
                // animate the merge first and the player can still cancel it.
                State = CraftingState.Crafting;
                OnCraftingStarted?.Invoke(_stagedItems);
            }

            return true;
        }

        public bool ResolveCraft()
        {
            if (State != CraftingState.Crafting)
            {
                return false;
            }

            Combine();
            return true;
        }

        public bool TryUnstage(int instanceId)
        {
            if (State != CraftingState.Selecting && State != CraftingState.Crafting)
            {
                return false;
            }

            int index = -1;
            for (int i = 0; i < _stagedItems.Count; i++)
            {
                if (_stagedItems[i].InstanceId == instanceId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return false;
            }

            var item = _stagedItems[index];
            _stagedItems.RemoveAt(index);
            // Leaving Crafting here cancels the pending combine; the count is now
            // below the combine threshold, so it cannot immediately re-trigger.
            State = _stagedItems.Count > 0 ? CraftingState.Selecting : CraftingState.Idle;
            _inventory.Return(item);
            OnItemUnstaged?.Invoke(item);
            return true;
        }

        public bool TryCollectResult()
        {
            if (State != CraftingState.ResultReady)
            {
                return false;
            }

            var result = PendingResult;
            PendingResult = null;
            State = CraftingState.Idle;
            _inventory.Return(result);
            OnResultCollected?.Invoke(result);
            return true;
        }

        public void ReturnAll()
        {
            var returned = new List<ArtifactInstance>(_stagedItems);
            if (PendingResult != null)
            {
                returned.Add(PendingResult);
            }

            _stagedItems.Clear();
            PendingResult = null;
            State = CraftingState.Idle;

            foreach (var instance in returned)
            {
                _inventory.Return(instance);
            }

            if (returned.Count > 0)
            {
                OnSessionCleared?.Invoke(returned);
            }
        }

        private void Combine()
        {
            var inputIds = new string[_stagedItems.Count];
            for (int i = 0; i < _stagedItems.Count; i++)
            {
                inputIds[i] = _stagedItems[i].DefinitionId;
            }

            var result = _fusionResolver.Resolve(inputIds);
            _stagedItems.Clear();
            PendingResult = _inventory.CreateDetachedInstance(result.OutputDefinitionId);
            State = CraftingState.ResultReady;
            OnCraftSucceeded?.Invoke(PendingResult, result.IsSignature);
        }
    }
}
