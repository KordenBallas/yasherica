using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Grounds combat units on the battlefield surface. <see cref="IHexGrid.HexToWorld"/> returns
    /// the true surface top, so a unit whose pivot is not at its feet must be raised by its
    /// pivot-to-feet distance. The offset is derived from the unit's own authored collider, so
    /// data-driven units ground correctly without hand-tuned per-prefab numbers; this assumes the
    /// model's visual feet coincide with the collider bottom (platform-generation.md §6).
    /// </summary>
    public static class UnitGrounding
    {
        /// <summary>
        /// Pivot-to-feet distance for a capsule-like collider: how far the collider bottom sits
        /// below the transform pivot, in world units.
        /// </summary>
        public static float FeetOffset(float colliderHeight, float colliderCenterY, float scaleY)
        {
            return (colliderHeight * 0.5f - colliderCenterY) * scaleY;
        }

        /// <summary>
        /// Derives the feet offset from the unit's authored collider (CharacterController first,
        /// then CapsuleCollider); 0 when neither is present. Init-time only — not a hot path.
        /// </summary>
        public static float FeetOffsetFor(Transform unit)
        {
            var controller = unit.GetComponent<CharacterController>();
            if (controller != null)
            {
                return FeetOffset(controller.height, controller.center.y, unit.lossyScale.y);
            }

            var capsule = unit.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                return FeetOffset(capsule.height, capsule.center.y, unit.lossyScale.y);
            }

            return 0f;
        }

        /// <summary>World position that puts the unit's feet on the given cell top.</summary>
        public static Vector3 Grounded(Vector3 cellTop, float feetOffset)
        {
            return cellTop + Vector3.up * feetOffset;
        }
    }
}
