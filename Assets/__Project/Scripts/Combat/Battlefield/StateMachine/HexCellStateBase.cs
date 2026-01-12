using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Base class for HexCell states providing default implementations.
    /// Follows the same pattern as PlatformStateBase.
    /// Concrete states can inherit from this to reduce boilerplate.
    /// </summary>
    public abstract class HexCellStateBase : IHexCellState
    {
        /// <summary>
        /// Gets the state type. Must be implemented by concrete states.
        /// </summary>
        public abstract HexCellStateType StateType { get; }

        /// <summary>
        /// Called when entering this state. Override to add custom behavior.
        /// </summary>
        public virtual void OnEnter(IHexCell cell) { }

        /// <summary>
        /// Called every frame while in this state. Override for animated states.
        /// </summary>
        public virtual void OnUpdate(IHexCell cell) { }

        /// <summary>
        /// Called when exiting this state. Override to add cleanup logic.
        /// </summary>
        public virtual void OnExit(IHexCell cell) { }

        /// <summary>
        /// Default transition validation: allow all transitions except to self of same type.
        /// Override to implement specific transition rules.
        /// </summary>
        public virtual bool CanTransitionTo(IHexCellState targetState)
        {
            // Default: allow all transitions except to self of same type
            return targetState?.StateType != this.StateType;
        }

        /// <summary>
        /// Gets the color for this state. Must be implemented by concrete states.
        /// </summary>
        public abstract Color GetColor();
    }
}
