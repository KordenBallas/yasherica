using System;
using Loot.Data.Definitions;
using UnityEngine;

namespace Loot.View
{
    /// <summary>
    /// World-space artifact pickup visual. The presenter decides what a body
    /// entering the trigger means; the view only renders and animates.
    /// </summary>
    public interface IWorldArtifactView
    {
        /// <summary>Raised when any collider enters the pickup trigger.</summary>
        event Action<Transform> OnBodyEntered;

        /// <summary>Raised when the underlying GameObject is destroyed.</summary>
        event Action OnViewDestroyed;

        void Configure(Sprite icon, LootConfig config);

        void SetInteractable(bool interactable);

        /// <summary>Grow-then-shrink pickup animation; invokes onComplete when finished.</summary>
        void PlayPickupAnimation(Transform moveTarget, Action onComplete);

        /// <summary>Feedback for a refused pickup (inventory cannot accept the item).</summary>
        void PlayRejectFeedback();

        void DestroySelf();
    }
}
