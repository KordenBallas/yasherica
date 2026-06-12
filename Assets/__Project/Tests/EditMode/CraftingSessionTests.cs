using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CraftingSessionTests
    {
        private const int ItemsToCombine = 2;

        private InventoryModel _inventory;
        private CraftingSession _session;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
            var recipes = new List<RecipeData>
            {
                new RecipeData(new[] { "fire", "water" }, "snake")
            };
            _session = new CraftingSession(new RecipeBook(recipes), _inventory, ItemsToCombine);
        }

        private void SelectAndResolve(int firstInstanceId, int secondInstanceId)
        {
            _session.TrySelect(firstInstanceId);
            _session.TrySelect(secondInstanceId);
            _session.ResolveCraft();
        }

        [Test]
        public void Constructor_ItemsToCombineBelowTwo_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new CraftingSession(new RecipeBook(new List<RecipeData>()), _inventory, 1));
        }

        [Test]
        public void TrySelect_FirstItem_MovesToSelectingAndRemovesFromInventory()
        {
            var fire = _inventory.Add("fire");

            ArtifactInstance staged = null;
            _session.OnItemStaged += i => staged = i;

            Assert.IsTrue(_session.TrySelect(fire.InstanceId));
            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.AreSame(fire, staged);
            Assert.AreEqual(1, _session.StagedItems.Count);
        }

        [Test]
        public void TrySelect_UnknownInstance_ReturnsFalse()
        {
            Assert.IsFalse(_session.TrySelect(999));
            Assert.AreEqual(CraftingState.Idle, _session.State);
        }

        [Test]
        public void TrySelect_NthItem_EntersCraftingAndRaisesOnCraftingStarted()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");

            IReadOnlyList<ArtifactInstance> craftingItems = null;
            bool resolvedEarly = false;
            _session.OnCraftingStarted += items => craftingItems = items;
            _session.OnCraftSucceeded += _ => resolvedEarly = true;
            _session.OnCraftFailed += _ => resolvedEarly = true;

            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            Assert.AreEqual(CraftingState.Crafting, _session.State);
            Assert.IsNotNull(craftingItems);
            Assert.AreEqual(2, craftingItems.Count);
            Assert.AreSame(fire, craftingItems[0]);
            Assert.AreSame(water, craftingItems[1]);
            Assert.IsNull(_session.PendingResult);
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.IsFalse(resolvedEarly);
        }

        [Test]
        public void TrySelect_WhileCrafting_IsRejected()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            var rock = _inventory.Add("rock");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            Assert.IsFalse(_session.TrySelect(rock.InstanceId));
            Assert.AreEqual(CraftingState.Crafting, _session.State);
            Assert.AreEqual(1, _inventory.Items.Count);
        }

        [Test]
        public void ResolveCraft_AfterNthItem_SuccessProducesResult()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");

            ArtifactInstance result = null;
            _session.OnCraftSucceeded += i => result = i;

            SelectAndResolve(fire.InstanceId, water.InstanceId);

            Assert.AreEqual(CraftingState.ResultReady, _session.State);
            Assert.IsNotNull(result);
            Assert.AreEqual("snake", result.DefinitionId);
            Assert.AreSame(result, _session.PendingResult);
            // Inputs are consumed: neither staged nor back in the inventory.
            Assert.AreEqual(0, _session.StagedItems.Count);
            Assert.AreEqual(0, _inventory.Items.Count);
        }

        [Test]
        public void ResolveCraft_Failure_ReturnsOnlyLastSelectedItemToInventory()
        {
            var fire = _inventory.Add("fire");
            var rock = _inventory.Add("rock");

            ArtifactInstance returned = null;
            _session.OnCraftFailed += i => returned = i;

            SelectAndResolve(fire.InstanceId, rock.InstanceId);

            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreSame(rock, returned);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(rock, _inventory.Items[0]);
            Assert.AreEqual(1, _session.StagedItems.Count);
            Assert.AreSame(fire, _session.StagedItems[0]);
        }

        [Test]
        public void ResolveCraft_FailureThenValidSecondItem_Succeeds()
        {
            var fire = _inventory.Add("fire");
            var rock = _inventory.Add("rock");
            var water = _inventory.Add("water");

            SelectAndResolve(fire.InstanceId, rock.InstanceId);

            Assert.IsTrue(_session.TrySelect(water.InstanceId));
            Assert.IsTrue(_session.ResolveCraft());
            Assert.AreEqual(CraftingState.ResultReady, _session.State);
            Assert.AreEqual("snake", _session.PendingResult.DefinitionId);
        }

        [Test]
        public void ResolveCraft_WhenIdle_ReturnsFalse()
        {
            Assert.IsFalse(_session.ResolveCraft());
            Assert.AreEqual(CraftingState.Idle, _session.State);
        }

        [Test]
        public void ResolveCraft_WhenSelecting_ReturnsFalse()
        {
            var fire = _inventory.Add("fire");
            _session.TrySelect(fire.InstanceId);

            Assert.IsFalse(_session.ResolveCraft());
            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreEqual(1, _session.StagedItems.Count);
        }

        [Test]
        public void ResolveCraft_WhenResultReady_ReturnsFalse()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            SelectAndResolve(fire.InstanceId, water.InstanceId);

            Assert.IsFalse(_session.ResolveCraft());
            Assert.AreEqual(CraftingState.ResultReady, _session.State);
        }

        [Test]
        public void TryUnstage_Selecting_ReturnsItemToInventoryAndGoesIdle()
        {
            var fire = _inventory.Add("fire");
            _session.TrySelect(fire.InstanceId);

            ArtifactInstance unstaged = null;
            _session.OnItemUnstaged += i => unstaged = i;

            Assert.IsTrue(_session.TryUnstage(fire.InstanceId));
            Assert.AreEqual(CraftingState.Idle, _session.State);
            Assert.AreEqual(0, _session.StagedItems.Count);
            Assert.AreSame(fire, unstaged);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(fire, _inventory.Items[0]);
        }

        [Test]
        public void TryUnstage_Crafting_CancelsCraft_OtherItemStaysStaged()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            Assert.IsTrue(_session.TryUnstage(water.InstanceId));
            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreEqual(1, _session.StagedItems.Count);
            Assert.AreSame(fire, _session.StagedItems[0]);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(water, _inventory.Items[0]);
        }

        [Test]
        public void TryUnstage_Crafting_ThenResolveCraft_ReturnsFalse()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);
            _session.TryUnstage(water.InstanceId);

            // A stale merge-animation completion must not resolve a cancelled craft.
            Assert.IsFalse(_session.ResolveCraft());
            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreEqual(1, _session.StagedItems.Count);
            Assert.AreEqual(1, _inventory.Items.Count);
        }

        [Test]
        public void TryUnstage_Crafting_ThenNewSelection_RestartsCrafting()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);
            _session.TryUnstage(water.InstanceId);

            int craftingStartedCount = 0;
            _session.OnCraftingStarted += _ => craftingStartedCount++;

            Assert.IsTrue(_session.TrySelect(water.InstanceId));
            Assert.AreEqual(CraftingState.Crafting, _session.State);
            Assert.AreEqual(1, craftingStartedCount);
        }

        [Test]
        public void TryUnstage_UnknownId_ReturnsFalse()
        {
            var fire = _inventory.Add("fire");
            _session.TrySelect(fire.InstanceId);

            Assert.IsFalse(_session.TryUnstage(999));
            Assert.AreEqual(CraftingState.Selecting, _session.State);
            Assert.AreEqual(1, _session.StagedItems.Count);
        }

        [Test]
        public void TryUnstage_WhileIdle_ReturnsFalse()
        {
            Assert.IsFalse(_session.TryUnstage(1));
            Assert.AreEqual(CraftingState.Idle, _session.State);
        }

        [Test]
        public void TryUnstage_WhileResultReady_ReturnsFalse()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            SelectAndResolve(fire.InstanceId, water.InstanceId);

            Assert.IsFalse(_session.TryUnstage(fire.InstanceId));
            Assert.AreEqual(CraftingState.ResultReady, _session.State);
        }

        [Test]
        public void TryCollectResult_AddsResultToInventoryAndResetsToIdle()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            SelectAndResolve(fire.InstanceId, water.InstanceId);

            ArtifactInstance collected = null;
            _session.OnResultCollected += i => collected = i;

            Assert.IsTrue(_session.TryCollectResult());
            Assert.AreEqual(CraftingState.Idle, _session.State);
            Assert.IsNull(_session.PendingResult);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreEqual("snake", _inventory.Items[0].DefinitionId);
            Assert.AreSame(_inventory.Items[0], collected);
        }

        [Test]
        public void TryCollectResult_WithoutPendingResult_ReturnsFalse()
        {
            Assert.IsFalse(_session.TryCollectResult());
        }

        [Test]
        public void TrySelect_WhileResultReady_IsRejected()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            var rock = _inventory.Add("rock");
            SelectAndResolve(fire.InstanceId, water.InstanceId);

            Assert.AreEqual(CraftingState.ResultReady, _session.State);
            Assert.IsFalse(_session.TrySelect(rock.InstanceId));
            Assert.AreEqual(1, _inventory.Items.Count);
        }

        [Test]
        public void ReturnAll_RestoresStagedItemsToInventory()
        {
            var fire = _inventory.Add("fire");
            _session.TrySelect(fire.InstanceId);

            IReadOnlyList<ArtifactInstance> cleared = null;
            _session.OnSessionCleared += items => cleared = items;

            _session.ReturnAll();

            Assert.AreEqual(CraftingState.Idle, _session.State);
            Assert.AreEqual(0, _session.StagedItems.Count);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(fire, _inventory.Items[0]);
            Assert.AreEqual(1, cleared.Count);
        }

        [Test]
        public void ReturnAll_FromCrafting_ReturnsAllStagedAndGoesIdle()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            IReadOnlyList<ArtifactInstance> cleared = null;
            _session.OnSessionCleared += items => cleared = items;

            _session.ReturnAll();

            Assert.AreEqual(CraftingState.Idle, _session.State);
            Assert.AreEqual(0, _session.StagedItems.Count);
            Assert.AreEqual(2, _inventory.Items.Count);
            Assert.AreEqual(2, cleared.Count);
            Assert.IsFalse(_session.ResolveCraft());
        }

        [Test]
        public void ReturnAll_RestoresUncollectedResultToInventory()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            SelectAndResolve(fire.InstanceId, water.InstanceId);

            _session.ReturnAll();

            Assert.AreEqual(CraftingState.Idle, _session.State);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreEqual("snake", _inventory.Items[0].DefinitionId);
        }

        [Test]
        public void ReturnAll_WhenSessionEmpty_DoesNotRaiseClearedEvent()
        {
            bool raised = false;
            _session.OnSessionCleared += _ => raised = true;

            _session.ReturnAll();

            Assert.IsFalse(raised);
        }
    }
}
