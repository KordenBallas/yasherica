using System;
using System.Collections.Generic;
using Core.Logging;
using GameInput.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace GameInput.View
{
    /// <summary>
    /// Loads the single <c>GameActions.inputactions</c> asset from Resources, resolves each named
    /// <see cref="GameAction"/> to its runtime <see cref="InputAction"/>, and enables the maps for the
    /// scene's lifetime. The one place that knows the asset path and the action-name mapping; everyone
    /// else consumes <see cref="IGameActions"/>.
    /// </summary>
    public sealed class GameActionsProvider : IGameActions, IInitializable, IDisposable
    {
        private const string AssetResourcePath = "Input/GameActions";

        private readonly IGameLogger _logger;
        private readonly Dictionary<GameAction, InputAction> _actions = new Dictionary<GameAction, InputAction>();
        private InputActionAsset _asset;

        public GameActionsProvider(IGameLogger logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            _asset = Resources.Load<InputActionAsset>(AssetResourcePath);
            if (_asset == null)
            {
                _logger.Error(LogCategory.Core,
                    $"[GameActionsProvider] No InputActionAsset at Resources/{AssetResourcePath}; input actions are dead.");
                return;
            }

            MapAction(GameAction.Move, "Player/Move");
            MapAction(GameAction.Interact, "Player/Interact");
            MapAction(GameAction.Aim, "Player/Aim");
            MapAction(GameAction.Fire, "Player/VolleyFire");
            MapAction(GameAction.CombatMoveMode, "Player/MoveMode");
            MapAction(GameAction.CombatChangeDirection, "Player/ChangeDirection");
            MapAction(GameAction.AbilitySlot1, "Player/Ability1");
            MapAction(GameAction.AbilitySlot2, "Player/Ability2");
            MapAction(GameAction.AbilitySlot3, "Player/Ability3");
            MapAction(GameAction.AbilitySlot4, "Player/Ability4");
            MapAction(GameAction.AbilitySlot5, "Player/Ability5");
            MapAction(GameAction.AbilitySlot6, "Player/Ability6");
            MapAction(GameAction.Confirm, "UI/Submit");
            MapAction(GameAction.Cancel, "UI/Cancel");
            MapAction(GameAction.Navigate, "UI/Navigate");

            _asset.Enable();
        }

        public void Dispose()
        {
            if (_asset != null)
            {
                _asset.Disable();
            }
        }

        public InputAction Get(GameAction action)
        {
            return _actions.TryGetValue(action, out var inputAction) ? inputAction : null;
        }

        private void MapAction(GameAction action, string actionPath)
        {
            var inputAction = _asset.FindAction(actionPath);
            if (inputAction == null)
            {
                _logger.Error(LogCategory.Core,
                    $"[GameActionsProvider] Action '{actionPath}' missing from {AssetResourcePath} — {action} is unbound.");
                return;
            }

            _actions[action] = inputAction;
        }
    }
}
